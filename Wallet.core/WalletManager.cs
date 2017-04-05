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
		private HashDictionary<TransactionValidation.PointedTransaction> _TxCache = new HashDictionary<TransactionValidation.PointedTransaction>();
		private DBContext _DBContext;
		private BlockChain.BlockChain _BlockChain;
		private TxStore _TxStore;
		private KeyStore _KeyStore { get; set; }
		private List<Key> _Keys;

		//TODO: consider not using thread loops - * watchout from dbreeze threading limitation
		private EventLoopMessageListener<BlockChainMessage> _BlockChainListener;

		public TxDeltaItemsEventArgs TxDeltaList { get; private set; }
		public AssetsMetadata AssetsMetadata { get; private set; }

		public event Action<ResetEventArgs> OnReset;
		public event Action<TxDeltaItemsEventArgs> OnItems;

		public WalletManager(BlockChain.BlockChain blockChain, string dbName)
		{
			_BlockChain = blockChain;

			_DBContext = new DBContext(dbName);

			_KeyStore = new KeyStore();
			_TxStore = new TxStore();

			TxDeltaList = new TxDeltaItemsEventArgs();
			AssetsMetadata = new AssetsMetadata();

			_BlockChainListener = new EventLoopMessageListener<BlockChainMessage>(OnBlockChainMessage);
			OwnResource(MessageProducer<BlockChainMessage>.Instance.AddMessageListener(_BlockChainListener));
			OwnResource(_DBContext);

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				_Keys = _KeyStore.List(dbTx);
				_TxStore.All(dbTx).Select(t=>t.Value).ToList().ForEach(txData =>
				{
					switch (txData.TxState)
					{
						case TxStateEnum.Confirmed:
							TxDeltaList.Add(new TxDelta(txData.TxState, txData.TxHash, txData.Tx, txData.AssetDeltas, txData.DateTime));
							break;
						case TxStateEnum.Unconfirmed:
							_BlockChain.HandleTransaction(txData.Tx);
							break;
					}
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

			HashDictionary<List<Types.Output>> txOutputs;
			HashDictionary<Types.Transaction> txs;
			_BlockChain.GetUTXOSet(IsMatch, out txOutputs, out txs); // TODO: use linq, return enumerator, remove predicate

			TxDeltaList.Clear();

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				//dbTx.Transaction.SynchronizeTables(TxBalancesStore.INDEXES);
				_TxStore.Reset(dbTx);

				foreach (var item in txOutputs)
				{
					var assetDeltas = new AssetDeltas();

					foreach (var output in item.Value)
					{
						AddOutput(assetDeltas, output);
					}

					_TxStore.Put(dbTx, item.Key, txs[item.Key], assetDeltas, TxStateEnum.Confirmed);
					TxDeltaList.Add(new TxDelta(TxStateEnum.Confirmed, item.Key, txs[item.Key], assetDeltas));
				}

				_BlockChain.memPool.TxPool.ToList().ForEach(t => HandleTx(dbTx, t.Key, t.Value, TxDeltaList, TxStateEnum.Unconfirmed));

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
				if (m is TxMessage)
				{
					var newTxStateMessage = m as TxMessage;
					HandleTx(dbTx, newTxStateMessage.TxHash, newTxStateMessage.Ptx, deltas, newTxStateMessage.State);
					dbTx.Commit();
				}
				else if (m is BlockMessage)
				{
					foreach (var item in (m as BlockMessage).PointedTransactions)
					{
						HandleTx(dbTx, item.Key, item.Value, deltas, TxStateEnum.Confirmed);
					}

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

		private void HandleTx(TransactionContext dbTx, byte[] txHash, TransactionValidation.PointedTransaction ptx, TxDeltaItemsEventArgs deltas, TxStateEnum txState)
		{
			var isValid = txState != TxStateEnum.Invalid;
			var _deltas = new AssetDeltas();

			if (!isValid && ptx == null)
			{
				if (_TxCache.ContainsKey(txHash))
					ptx = _TxCache[txHash];
				else
					return;
			}
			else
			{
				_TxCache[txHash] = ptx;
			}

			ptx.outputs.Where(IsMatch).ToList().ForEach(o => AddOutput(_deltas, o, !isValid));
			ptx.pInputs.ToList().ForEach(pInput =>
			{
				var key = GetKey(pInput.Item2);

				if (key != null)
				{
					AddOutput(_deltas, pInput.Item2, isValid);
					_KeyStore.Used(dbTx, key, true);
				}
			});

			if (_deltas.Count > 0)
			{
				var tx = TransactionValidation.unpoint(ptx);

				_TxStore.Put(dbTx, txHash, tx, _deltas, txState);
				deltas.Add(new TxDelta(txState, txHash, tx, _deltas));
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

			var spendableOutputs = new List<Types.Output>();

			_TxStore.All(dbTx).Select(t=>t.Value).ToList().ForEach(txData =>
			{
				uint idx = 0;
				txData.Tx.outputs.ToList().ForEach(o =>
				{
					if (o.spend.asset.SequenceEqual(asset))
					{
						var key = GetKey(o);

						if (key != null)
						{
							if (txData.TxState != TxStateEnum.Invalid)
							{
								matchingAssets.Add(new Asset()
								{
									Key = key,
									TxState = txData.TxState,
									Outpoint = new Types.Outpoint(txData.TxHash, idx),
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
					bool canSpend = false;
					switch (matchingAsset.TxState)
					{
						case TxStateEnum.Confirmed:
							canSpend = _BlockChain.UTXOStore.ContainsKey(
								context, matchingAsset.Outpoint.txHash, matchingAsset.Outpoint.index) &&
								!_BlockChain.memPool.TxPool.ContainsOutpoint(matchingAsset.Outpoint);
							break;
						case TxStateEnum.Unconfirmed:
							canSpend = !_BlockChain.memPool.TxPool.ContainsOutpoint(matchingAsset.Outpoint) &&
								_BlockChain.memPool.TxPool.Contains(matchingAsset.Outpoint.txHash);
							break;
					}

					WalletTrace.Verbose($"require: output with amount {matchingAsset.Output.spend.amount} spendable: {canSpend}");

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

		public bool CanSpend(byte[] asset, ulong amount)
		{
			using (TransactionContext dbTx = _DBContext.GetTransactionContext())
			{
				ulong change;
				Assets assets;

				return Require(dbTx, asset, amount, out change, out assets);
			}
		}

		public bool Parse(byte[] rawTxBytes, out Types.Transaction tx)
		{
			try
			{
				tx = Serialization.context.GetSerializer<Types.Transaction>().UnpackSingleObject(rawTxBytes);
				return true;
			}
			catch
			{
				tx = null;
				return false;
			}
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

		/// <summary>
		/// Constract and sign a transaction activating a contract
		/// </summary>
		/// <returns>The sign.</returns>
		/// <param name="address">Address.</param>
		/// <param name="asset">Asset.</param>
		/// <param name="amount">Amount.</param>
		public bool SacrificeToContract(byte[] contractHash, byte[] code, ulong zenAmount, out Types.Transaction signedTx)
		{
			ulong change;
			Assets assets;

			var outputs = new List<Types.Output>();

			using (TransactionContext dbTx = _DBContext.GetTransactionContext())
			{
				if (!Require(dbTx, Tests.zhash, zenAmount, out change, out assets))
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

					outputs.Add(new Types.Output(Types.OutputLock.NewPKLock(key.Address), new Types.Spend(Tests.zhash, change)));
				}
			}

			var output = new Types.Output(
				Types.OutputLock.NewContractSacrificeLock(
					new Types.LockCore(0, ListModule.OfSeq(new byte[][] { contractHash }))
				),
				new Types.Spend(Tests.zhash, zenAmount)
			);

			outputs.Add(output);

			signedTx = TransactionValidation.signTx(new Types.Transaction(
				1,
				ListModule.OfSeq(assets.Select(t => t.Outpoint)),
				ListModule.OfSeq(new List<byte[]>()),
				ListModule.OfSeq(outputs),
				new Microsoft.FSharp.Core.FSharpOption<Types.ExtendedContract>(
					Types.ExtendedContract.NewContract(new Types.Contract(code, new byte[] { }, new byte[] { }))
				)
			  ), ListModule.OfSeq(assets.Select(i => i.Key.Private)));

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

		public bool IsContractActive(byte[] contractHash)
		{
			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				return new ActiveContractSet().IsActive(dbTx, contractHash);
			}
		}

		public bool IsContractActive(byte[] contractHash, out UInt32 nextBlocks)
		{
			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				bool isActive = new ActiveContractSet().IsActive(dbTx, contractHash);

				var lastBlock = isActive ? new ActiveContractSet().LastBlock(dbTx, contractHash) : 0;

				var currentHeight = _BlockChain.Tip == null ? 0 : _BlockChain.Tip.Value.header.blockNumber;

				nextBlocks = currentHeight - lastBlock;

				return isActive;
			}
		}
	}
}