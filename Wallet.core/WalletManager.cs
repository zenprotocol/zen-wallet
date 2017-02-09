using System;
using System.Collections.Generic;
using Infrastructure;
using Store;
using System.Linq;
using Wallet.core.Store;
using Wallet.core.Data;
using Consensus;
using BlockChain.Data;
using BlockChain;
using Microsoft.FSharp.Collections;

namespace Wallet.core
{
	public class WalletManager : ResourceOwner
	{
		private DBContext _DBContext;
		private BlockChain.BlockChain _BlockChain;
		private TxBalancesStore _TxBalancesStore;
		private KeyStore _KeyStore { get; set; }
		private List<Key> _Keys;

		//TODO: merge, consider not using thread loops - * watchout from dbreeze threading limitation
		private EventLoopMessageListener<BlockChainMessage> _BlockChainListener;

		public TxDeltaItemsEventArgs TxDeltaList { get; private set; }

		public event Action<ResetEventArgs> OnReset;
		public event Action<TxDeltaItemsEventArgs> OnItems;

		public WalletManager(BlockChain.BlockChain blockChain, string dbName)
		{
			_BlockChain = blockChain;

			_DBContext = new DBContext(dbName);

			_KeyStore = new KeyStore();
			_TxBalancesStore = new TxBalancesStore();

			TxDeltaList = new TxDeltaItemsEventArgs();

			_BlockChainListener = new EventLoopMessageListener<BlockChainMessage>(OnBlockChainMessage);
			OwnResource(MessageProducer<BlockChainMessage>.Instance.AddMessageListener(_BlockChainListener));
			OwnResource(_DBContext);

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				_Keys = _KeyStore.List(dbTx);
				_TxBalancesStore.All(dbTx).ToList().ForEach(t =>
				{
					var state = _TxBalancesStore.TxState(dbTx, t.Key);

					if (state != TxStateEnum.Invalid)
						TxDeltaList.Add(new TxDelta(state, t.Value, _TxBalancesStore.Balances(dbTx, t.Key)));
				});
			}
		}

		/// <summary>
		/// Imports wallet file 
		/// </summary>
		public void Import(Key key)
		{
			_BlockChainListener.Pause();

			using (var context = _DBContext.GetTransactionContext())
			{
				_KeyStore.AddKey(context, key.PrivateAsString);
				context.Commit();
			}

			_Keys.Add(key);

			var utxoSetTxs = _BlockChain.GetUTXOSet(IsMatch); // TODO: use linq, return enumerator, remove predicate

			TxDeltaList.Clear();
			using (var dbTx = _DBContext.GetTransactionContext())
			{
				dbTx.Transaction.SynchronizeTables(TxBalancesStore.INDEXES);
				_TxBalancesStore.Reset(dbTx);

				foreach (var tx in utxoSetTxs)
				{
					var balances = new AssetDeltas();

					tx.Key.Value.outputs.Where(IsMatch).ToList().ForEach(o => AddOutput(balances, o));
					_TxBalancesStore.Put(dbTx, tx.Key, balances, TxStateEnum.Confirmed);
					TxDeltaList.Add(new TxDelta(TxStateEnum.Confirmed, tx.Key.Value, balances));
				}

				_BlockChain.TxMempool.GetAll().ForEach(ptx => HandleTx(dbTx, ptx, TxDeltaList, TxStateEnum.Unconfirmed));

				dbTx.Commit();
			}

			if (OnReset != null)
				OnReset(new ResetEventArgs() { TxDeltaList = TxDeltaList });

			_BlockChainListener.Continue();
		}

		private void OnBlockChainMessage(BlockChainMessage m)
		{
			var deltas = new TxDeltaItemsEventArgs();

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				if (m is NewTxMessage)
				{
					HandleTx(dbTx, (m as NewTxMessage).Tx, deltas, (m as NewTxMessage).State);
					dbTx.Commit();
				}
				else if (m is NewBlockMessage)
				{
					(m as NewBlockMessage).PointedTransactions.ForEach(tx =>
					{
						HandleTx(dbTx, tx, deltas, TxStateEnum.Confirmed);
					});

					dbTx.Commit();
				}
			}

			if (deltas.Count > 0)
			{
				TxDeltaList.AddRange(deltas);

				if (OnItems != null)
					OnItems(deltas);
			}
		}

		private void HandleTx(TransactionContext dbTx, TransactionValidation.PointedTransaction ptx, TxDeltaItemsEventArgs deltas, TxStateEnum txState)
		{
			var _deltas = new AssetDeltas();

			ptx.outputs.Where(IsMatch).ToList().ForEach(o => AddOutput(_deltas, o));
			ptx.pInputs.ToList().ForEach(pInput =>
			{
				var key = GetKey(pInput.Item2);

				if (key != null)
				{
					AddOutput(_deltas, pInput.Item2, true);
					_KeyStore.Used(dbTx, key, true);
				}
			});

			if (_deltas.Count > 0)
			{
				var tx = TransactionValidation.unpoint(ptx);
				var keyedTx = new Keyed<Types.Transaction>(Merkle.transactionHasher.Invoke(tx), tx);

				if (_TxBalancesStore.ContainsKey(dbTx, keyedTx.Key))
				{
					_TxBalancesStore.SetTxState(dbTx, keyedTx.Key, txState);
				}
				else
				{
					_TxBalancesStore.Put(dbTx, keyedTx, _deltas, txState);
				}

				//dbTx.Commit();

				deltas.Add(new TxDelta(txState, TransactionValidation.unpoint(ptx), _deltas));
			}
		}

		private bool IsMatch(Tuple<Types.Outpoint, Types.Output> pointedOutput)
		{
			return IsMatch(pointedOutput.Item2);
		}

		private bool IsMatch(Types.Output output)
		{
			return GetKey(output) != null;
		}

		private Key GetKey(Types.Output output)
		{
			foreach (var key in _Keys)
			{
				if (key.IsMatch(output.@lock))
				{
					return key;
				}
			}

			return null;
		}

		private void AddOutput(AssetDeltas balances, Types.Output output, bool isSpending = false)
		{
			if (!balances.ContainsKey(output.spend.asset))
			{
				balances[output.spend.asset] = 0;
			}

			balances[output.spend.asset] += isSpending ? -1 * (long)output.spend.amount : (long)output.spend.amount;
		}

		//	public void Sync()
		//	{
		//_HandledTransactions.Clear();
		//var utxoSet = _BlockChain.GetUTXOSet();
		//WalletTrace.Information($"loading blockchain's {utxoSet.Count()} utxos");
		//var transactions = new List<Types.Transaction>();


		//var tipItr = _BlockChain.Tip.Key;

		//while (tipItr != null && !tipItr.SequenceEqual(new byte[] { }))
		//{
		//	using (var context = _DBContext.GetTransactionContext()) // TODO: encap
		//	{
		//		foreach (var transaction in _BlockChain.BlockStore.Transactions(context, tipItr))
		//		{
		//			transactions.Add(transaction.Value);
		//		}
		//	}

		//	tipItr = _BlockChain.GetBlockHeader(tipItr).parent;
		//}

		//using (var context = _DBContext.GetTransactionContext())
		//{
		//	//_OutpointAssetsStore.RemoveAll(context);
		//	_UTXOStore.RemoveAll(context);
		//	_BalanceStore.RemoveAll(context);

		//	foreach (var item in utxoSet)
		//	{
		//		if (_KeyStore.Find(context, item.Value, true))
		//		{
		//			_UTXOStore.Put(context, item);

		//			//_AssetsManager.Add(item);
		//			//AddToRunningBalance(item.Value);
		//			//if (!myTransactions.Contains(item.Item1.txHash))
		//			//{
		//			//	myTransactions.Add(item.Item1.txHash);
		//			//}
		//		}
		//	}

		//	context.Commit();
		//}

		//foreach (var transaction in transactions)
		//{
		//	HandleTransaction(new Keyed<Types.Transaction>(Merkle.transactionHasher.Invoke(transaction), transaction), true);
		//}
		//	}

		/// <summary>
		/// get a set of outpoints with matching keys using greedy algorithm 
		/// </summary>
		/// <returns>null if could not satisfy</returns>
		/// <param name="asset">Asset.</param>
		/// <param name="amount">Amount.</param>
		private bool Require(TransactionContext dbTx, byte[] asset, ulong amount, out ulong change, out Assets assets)
		{
			var matchingAssets = new Assets();

			_TxBalancesStore.All(dbTx).ToList().ForEach(t =>
			{
				uint idx = 0;
				t.Value.outputs.ToList().ForEach(o =>
				{
					if (o.spend.asset.SequenceEqual(asset))
					{
						var key = GetKey(o);

						if (key != null)
						{
							var txState = _TxBalancesStore.TxState(dbTx, t.Key);

							if (txState != TxStateEnum.Invalid)
							{
								matchingAssets.Add(new Asset()
								{
									Key = key,
									TxState = txState,
									Outpoint = new Types.Outpoint(t.Key, idx),
									Output = o
								});
							}
						}
					}
					idx++;
				});
			});

			var unspentMatchingAssets = new Assets();

			using (TransactionContext context = _BlockChain.GetDBTransaction())
			{
				foreach (Asset matchingAsset in matchingAssets)
				{
					byte[] outputKey = new byte[matchingAsset.Outpoint.txHash.Length + 1];
					matchingAsset.Outpoint.txHash.CopyTo(outputKey, 0);
					outputKey[matchingAsset.Outpoint.txHash.Length] = (byte)matchingAsset.Outpoint.index;

					bool canSpend = false;
					switch (matchingAsset.TxState)
					{
						case TxStateEnum.Confirmed:
							canSpend = _BlockChain.UTXOStore.ContainsKey(context, outputKey) &&
								!_BlockChain.TxMempool.ContainsOutpoint(matchingAsset.Outpoint);
							break;
						case TxStateEnum.Unconfirmed:
							canSpend = !_BlockChain.TxMempool.ContainsOutpoint(matchingAsset.Outpoint) &&
								_BlockChain.TxMempool.ContainsKey(matchingAsset.Outpoint.txHash);
							break;
					}

					if (canSpend)
					{
						unspentMatchingAssets.Add(matchingAsset);
					}
				}
			}

			ulong total = 0;
			assets = new Assets();

			foreach (var unspentMatchingAsset in unspentMatchingAssets)
			{
				if (total >= amount)
				{
					break;
				}

				assets.Add(unspentMatchingAsset);
				total += unspentMatchingAsset.Output.spend.amount;
			}

			change = total - amount;
			return total >= amount;
		}

		/// <summary>
		/// Spend the specified tx.
		/// </summary>
		/// <returns>The spend.</returns>
		/// <param name="tx">Tx.</param>
		public bool Transmit(Types.Transaction tx)
		{
			return _BlockChain.HandleTransaction(tx);
		}

		/// <summary>
		/// Constract and sign a transaction satisfying amount of asset
		/// </summary>
		/// <returns>The sign.</returns>
		/// <param name="address">Address.</param>
		/// <param name="asset">Asset.</param>
		/// <param name="amount">Amount.</param>
		public bool Sign(byte[] address, byte[] asset, ulong amount, out Types.Transaction signedTx)
		{
			ulong change;
			Assets assets;

			var outputs = new List<Types.Output>();

			using (TransactionContext dbTx = _DBContext.GetTransactionContext())
			{
				if (!Require(dbTx, asset, amount, out change, out assets))
				{
					signedTx = null;
					return false;
				}
				else if (change > 0)
				{
					Key key;

					if (_KeyStore.GetUnusedKey(dbTx, out key, true))
					{
						_Keys.Add(key);
						dbTx.Commit();
					}
			
					outputs.Add(new Types.Output(Types.OutputLock.NewPKLock(key.Address), new Types.Spend(asset, change)));
				}
			}

			outputs.Add(new Types.Output(Types.OutputLock.NewPKLock(address), new Types.Spend(Tests.zhash, amount)));

			signedTx = TransactionValidation.signTx(new Types.Transaction(
				1,
				ListModule.OfSeq(assets.Select(t => t.Outpoint)),
				ListModule.OfSeq(new List<byte[]>()),
				ListModule.OfSeq(outputs),
				null), ListModule.OfSeq(assets.Select(i => i.Key.Private)));

			return true;
		}

		public Key GetUnusedKey()
		{
			Key key;

			using (var context = _DBContext.GetTransactionContext())
			{
				if (_KeyStore.GetUnusedKey(context, out key))
				{
					_Keys.Add(key);
					context.Commit();
				}
			}

			return key;
		}
	}
}