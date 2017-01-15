using System;
using System.Collections.Generic;
using Infrastructure;
using Store;
using System.Linq;
using Wallet.core.Store;
using Wallet.core.Data;
using Consensus;
using Microsoft.FSharp.Collections;
using BlockChain.Data;
using BlockChain.Store;

namespace Wallet.core
{
	public class WalletManager : ResourceOwner
	{
		private DBContext _DBContext;
		private BlockChain.BlockChain _BlockChain;
		private TxHistoryStore _TxHistoryStore;
		private AssetsManager _AssetsManager;
		private KeyStore _KeyStore { get; set; }

		public List<TransactionSpendData> MyTransactions { get; private set; }

		public event Action<TransactionSpendData> OnNewTransaction;
		//public event Action OnSynced;

#if DEBUG
		public void Reset()
		{
			WalletTrace.Information("reset");
		}
#endif

		public WalletManager(BlockChain.BlockChain blockChain, string dbName)
		{
		//	WalletTrace.Information("Initializing");

			_BlockChain = blockChain;
			OwnResource(_BlockChain);

			_AssetsManager = new AssetsManager();
			_DBContext = new DBContext(dbName);
			OwnResource(_DBContext);

			_KeyStore = new KeyStore();

			using (var context = _DBContext.GetTransactionContext())
			{
				WalletTrace.Information($"Keystore contains {_KeyStore.List(context, false).Count} unused, {_KeyStore.List(context, true).Count} used");
			}


			OwnResource(MessageProducer<TxMempool.AddedMessage>.Instance.AddMessageListener(
				new EventLoopMessageListener<TxMempool.AddedMessage>(m =>
				{
					HandleTransaction(m.Transaction.Value);
				})
			));

			OwnResource(MessageProducer<TxStore.AddedMessage>.Instance.AddMessageListener(
				new EventLoopMessageListener<TxStore.AddedMessage>(m =>
				{
					TransactionConfirmet(m.Transaction.Value);
				})
			));


			Init();
		}

		private void UpdateAssets(TransactionSpendDataEx transactionSpendData, bool skipInputs = false, bool skipOutputs = false)
		{
			if (!skipOutputs)
			{
				foreach (var output in transactionSpendData.Outputs)
				{
					var txHash = Merkle.transactionHasher.Invoke(transactionSpendData.Transaction);
					var outpoint = new Types.Outpoint(txHash, (uint)output);
					_AssetsManager.Add(new Tuple<Types.Outpoint, Types.Output>(outpoint, transactionSpendData.Transaction.outputs[output]));
				}
			}

			if (!skipInputs)
			{
				foreach (var input in transactionSpendData.Inputs)
				{
					_AssetsManager.Remove(transactionSpendData.PointedTransaction.pInputs[input]);
				}
			}
		}

		public void Sync()
		{
		//	var transactions = _BlockChain.GetTransactions();

			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				#if TRACE
				int skippedTraceCount = 0;
				#endif

				foreach (Types.Transaction transaction in _BlockChain.GetTransactions())
				{
					var transactionSpendData = GetTransactionSpendData(context, transaction);

					if (transactionSpendData != null)
					{
						WalletTrace.Information($"Found tx. {transactionSpendData.Inputs.Count} inputs, {transactionSpendData.Outputs.Count} outputs");
						_TxHistoryStore.Put(context, new Keyed<Types.Transaction>(Merkle.transactionHasher.Invoke(transaction), transaction));
						InvalidateKeys(context, transactionSpendData.Keys);
						MyTransactions.Add(transactionSpendData);
						UpdateAssets(transactionSpendData);
					}
					#if TRACE
					else
					{
						skippedTraceCount++;
					}
					#endif
				}

				context.Commit();

				#if TRACE
				WalletTrace.Information("Skipped: " + skippedTraceCount);
				#endif
			}
		}

		//public void Sync()
		//{
		//	var tip = _BlockChain.Tip;

		//	if (tip == null)
		//	{
		//		return;
		//	}

		//	var block = tip.Value;

		//	using (TransactionContext context = _DBContext.GetTransactionContext())
		//	{
		//		while (block != null)
		//		{
		//			foreach (var transaction in block.transactions)
		//			{
		//				var transactionSpendData = GetTransactionSpendData(context, transaction);

		//				if (transactionSpendData != null)
		//				{
		//					_TxHistoryStore.Put(context, new Keyed<Types.Transaction>(Merkle.transactionHasher.Invoke(transaction), transaction));
		//					InvalidateKeys(context, transactionSpendData.Keys);
		//					AddToMemory(transactionSpendData);
		//				}
		//			}

		//			block = _BlockChain.GetBlock(block.header.parent);
		//		}

		//		context.Commit();
		//	}
		//}

		private void InvalidateKeys(TransactionContext context, List<Key> keys)
		{
			foreach (var key in keys)
			{
				WalletTrace.Information($"Key used: {key.PrivateAsString}");
				_KeyStore.Used(context, key);
			}
		}

		private void TransactionConfirmet(Types.Transaction transaction)
		{ 
		}

		private void HandleTransaction(Types.Transaction transaction)
		{
			TransactionSpendDataEx transactionSpendData = null;

			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				transactionSpendData = GetTransactionSpendData(context, transaction);

				if (transactionSpendData == null)
				{
					return;
				}

				InvalidateKeys(context, transactionSpendData.Keys);
				_TxHistoryStore.Put(context, new Keyed<Types.Transaction>(Merkle.transactionHasher.Invoke(transaction), transaction));
				context.Commit();
			}

			MyTransactions.Add(transactionSpendData);
			UpdateAssets(transactionSpendData);

			try //todo: get a grip of events vs. messages with respect to exception handling, blockage etc.
			{
				if (OnNewTransaction != null)
				{
					OnNewTransaction(transactionSpendData);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}

		private void Init()
		{
			MyTransactions = new List<TransactionSpendData>();

			_TxHistoryStore = new TxHistoryStore();

			using (var context = _DBContext.GetTransactionContext())
			{
				//foreach (var item in _BlockChain.GetUTXOSet())
				//{
				//	var key = _KeyStore.Find(context, item.Item2);

				//	if (key != null)
				//	{
				//		_AssetsManager.Add(item);
				//	}
				//}

	//			var x = _TxHistoryStore.All(context).Count();
				WalletTrace.Information("tx count: " + _TxHistoryStore.All(context).Count());

				foreach (var transaction in _TxHistoryStore.All(context))
				{
					var transactionSpendData = GetTransactionSpendData(context, transaction.Value);

					//if (transactionSpendData != null)
					//{
					MyTransactions.Add(transactionSpendData);
					UpdateAssets(transactionSpendData, skipInputs: true);
					//}
				}

				foreach (var transaction in _TxHistoryStore.All(context))
				{
					var transactionSpendData = GetTransactionSpendData(context, transaction.Value);

					//if (transactionSpendData != null)
					//{
					UpdateAssets(transactionSpendData, skipOutputs: true);
					//}
				}

			}
		}

		private TransactionSpendDataEx GetTransactionSpendData(TransactionContext context, Types.Transaction transaction)
		{
			var myOutputs = new List<Types.Output>();
			var myOutputIdxs = new List<int>();
			var myInputIdxs = new List<int>();
			var keys = new List<Key>();

			for (int i = 0; i < transaction.outputs.Count(); i++)
			{
				var output = transaction.outputs[i];
				var key = _KeyStore.Find(context, output);

				if (key != null)
				{
					myOutputIdxs.Add(i);
					keys.Add(key);
				}
			}

			for (int i = 0; i < transaction.inputs.Count(); i++)
			{
				var tx = _BlockChain.GetTransaction(transaction.inputs[i].txHash);
				var idx = transaction.inputs[i].index;
				var output = tx.outputs[(int)idx];

				myOutputs.Add(output);

				var key = _KeyStore.Find(context, output);

				if (key != null)
				{
					myInputIdxs.Add(i);
					keys.Add(key);
				}
			}

			return myOutputIdxs.Count == 0 && myInputIdxs.Count == 0 ? null : new TransactionSpendDataEx(
				transaction,
				myOutputs,
				myOutputIdxs,
				myInputIdxs,
				keys
			);
		}

		public bool Spend(string address, byte[] asset, ulong amount) //TODO: sign it.
		{
			try
			{
				var inputs = _AssetsManager.Get(asset, amount);

				if (inputs == null)
				{
					return false;
				}

				var outputs = new List<Types.Output>();

				outputs.Add(new Types.Output(Types.OutputLock.NewPKLock(Key.FromBase64String(address)), new Types.Spend(Tests.zhash, amount)));

				if (inputs.Item2 > 0)
				{
					outputs.Add(new Types.Output(Types.OutputLock.NewPKLock(GetUnsendKey(true).Address), new Types.Spend(Tests.zhash, inputs.Item2)));
				}

				var hashes = new List<byte[]>();
				var version = (uint)1;

				Types.Transaction transaction = new Types.Transaction(version,
					ListModule.OfSeq(inputs.Item1),
					ListModule.OfSeq(hashes),
					ListModule.OfSeq(outputs),
					null);

				//TODO:		Consensus.TransactionValidation.signTx(
				return _BlockChain.HandleNewTransaction(transaction);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return false;
			}
		}

		public bool AddKey(string base64EncodedPrivateKey)
		{
			bool result = false;

			using (var context = _DBContext.GetTransactionContext())
			{
				result = _KeyStore.AddKey(context, base64EncodedPrivateKey);
				context.Commit();
			}

			return result;
		}

		public Key GetUnsendKey(bool isChange = false)
		{
			Key result;

			using (var context = _DBContext.GetTransactionContext())
			{
				result = _KeyStore.GetUnsendKey(context, isChange);
				context.Commit();
			}

			return result;
		}

		public void Used(Key key)
		{
			using (var context = _DBContext.GetTransactionContext())
			{
				_KeyStore.Used(context, key);
				context.Commit();
			}
		}

		public List<Key> ListKeys(bool? used = null, bool? isChange = null)
		{
			using (var context = _DBContext.GetTransactionContext())
			{
				return _KeyStore.List(context, used, isChange);
			}
		}
	}
}
