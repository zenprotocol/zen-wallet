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

		private EventLoopMessageListener<TxAddedMessage> _BlockChainTxListener;
		private EventLoopMessageListener<BkAddedMessage> _BlockChainBkListener;

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

			_BlockChainTxListener = new EventLoopMessageListener<TxAddedMessage>(OnTxAddedMessage);
			_BlockChainBkListener = new EventLoopMessageListener<BkAddedMessage>(OnBkAddedMessage);

			OwnResource(MessageProducer<TxAddedMessage>.Instance.AddMessageListener(_BlockChainTxListener));
			OwnResource(MessageProducer<BkAddedMessage>.Instance.AddMessageListener(_BlockChainBkListener));

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				_Keys = _KeyStore.List(dbTx);
				_TxBalancesStore.All(dbTx).ToList().ForEach(t =>
				{
					TxDeltaList.Add(new TxDelta(_TxBalancesStore.TxState(dbTx, t.Key), t.Value, _TxBalancesStore.Balances(dbTx, t.Key)));
				});
			}

			OwnResource(_DBContext); //TODO
		}

		/// <summary>
		/// Imports wallet file 
		/// </summary>
		public void Import(Key key)
		{
			_BlockChainTxListener.Pause();
			_BlockChainBkListener.Pause();

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

				_BlockChain.TxMempool.GetAll().ForEach(ptx => HandleTx(dbTx, ptx, TxDeltaList, false));

				dbTx.Commit();
			}

			if (OnReset != null)
				OnReset(new ResetEventArgs() { TxDeltaList = TxDeltaList });

			_BlockChainBkListener.Continue();
			_BlockChainTxListener.Continue();
		}

		private void OnBkAddedMessage(BkAddedMessage m)
		{
			var txs = new List<TransactionValidation.PointedTransaction>();

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				m.Bk.transactions.ToList().ForEach(tx => txs.Add(_BlockChain.GetPointedTransaction(dbTx, tx)));
			}

			var deltas = new TxDeltaItemsEventArgs();

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				txs.ForEach(tx =>
				{
					HandleTx(dbTx, tx, deltas, true);
				});
			}

			if (deltas.Count > 0)
			{
				TxDeltaList.AddRange(deltas);

				if (OnItems != null)
					OnItems(deltas);
			}
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
		private Assets Require(byte[] asset, ulong amount, out ulong change)
		{
			var assets = new Assets();

			using (TransactionContext dbTx = _DBContext.GetTransactionContext())
			{
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
									assets.Add(new Asset()
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
			}

			ulong total = 0;
			var _assets = new Assets();

			foreach (var _asset in assets)
			{
				if (total >= amount)
				{
					break;
				}

				_assets.Add(_asset);
				total += _asset.Output.spend.amount;
			}

			change = total - amount;

			return total < amount ? null : assets;
		}

		/// <summary>
		/// Spend the specified tx.
		/// </summary>
		/// <returns>The spend.</returns>
		/// <param name="tx">Tx.</param>
		public bool Spend(Types.Transaction tx)
		{
			return _BlockChain.HandleNewTransaction(tx) == AddTx.Result.Added;
		}

		/// <summary>
		/// Constract and sign a transaction satisfying amount of asset
		/// </summary>
		/// <returns>The sign.</returns>
		/// <param name="address">Address.</param>
		/// <param name="asset">Asset.</param>
		/// <param name="amount">Amount.</param>
		public Types.Transaction Sign(byte[] address, byte[] asset, ulong amount)
		{
			ulong change;
			var outputs = new List<Types.Output>();

			var assets = Require(asset, amount, out change);

			if (assets == null)
			{
				return null;
			}
			else if (change > 0)
			{
				using (TransactionContext dbTx = _DBContext.GetTransactionContext())
				{
					var key = _KeyStore.GetUnusedKey(dbTx);
					outputs.Add(new Types.Output(Types.OutputLock.NewPKLock(key.Address), new Types.Spend(Tests.zhash, change)));

					_KeyStore.Used(dbTx, key, true); //TODO: necessary?
					dbTx.Commit();
				}
			}

			outputs.Add(new Types.Output(Types.OutputLock.NewPKLock(address), new Types.Spend(Tests.zhash, amount)));

			return TransactionValidation.signTx(new Types.Transaction(
				1,
				ListModule.OfSeq(assets.Select(t => t.Outpoint)),
				ListModule.OfSeq(new List<byte[]>()),
				ListModule.OfSeq(outputs),
				null), ListModule.OfSeq(assets.Select(i => i.Key.Private)));			
		}

		public Key GetUnusedKey()
		{
			Key key;

			using (var context = _DBContext.GetTransactionContext())
			{
				key = _KeyStore.GetUnusedKey(context);
				context.Commit();
			}

			_Keys.Add(key);

			return key;
		}

		private void OnTxAddedMessage(TxAddedMessage m)
		{
			var deltas = new TxDeltaItemsEventArgs();

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				HandleTx(dbTx, m.Tx, deltas, false);
			}

			if (deltas.Count > 0)
			{
				if (OnItems != null)
					OnItems(deltas);
			}
		}

		private void HandleTx(TransactionContext dbTx, TransactionValidation.PointedTransaction ptx, TxDeltaItemsEventArgs deltas, bool confirmed)
		{
			var _deltas = new AssetDeltas();

			ptx.outputs.Where(IsMatch).ToList().ForEach(o => AddOutput(_deltas, o));
			ptx.pInputs.Where(IsMatch).ToList().ForEach(o => AddPOutput(_deltas, o));

			if (_deltas.Count > 0)
			{
				var tx = TransactionValidation.unpoint(ptx);
				var keyedTx = new Keyed<Types.Transaction>(Merkle.transactionHasher.Invoke(tx), tx);

				var state = confirmed ? TxStateEnum.Confirmed : TxStateEnum.Unconfirmed;

				if (_TxBalancesStore.ContainsKey(dbTx, keyedTx.Key))
				{
					_TxBalancesStore.SetTxState(dbTx, keyedTx.Key, state);
				}
				else
				{
					_TxBalancesStore.Put(dbTx, keyedTx, _deltas, state);
				}
				dbTx.Commit();

				deltas.Add(new TxDelta(state, TransactionValidation.unpoint(ptx), _deltas));
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

			balances[output.spend.asset] += isSpending  ? -1 * (long)output.spend.amount : (long)output.spend.amount;
		}

		private void AddPOutput(AssetDeltas balances, Tuple<Types.Outpoint, Types.Output> pointedOutput)
		{
			AddOutput(balances, pointedOutput.Item2, true);
		}
	}
}