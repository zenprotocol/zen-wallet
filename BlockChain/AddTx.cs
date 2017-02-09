//using System;
//using System.Collections.Generic;
//using Store;
//using Consensus;
//using Microsoft.FSharp.Collections;
//using System.Linq;
//using Infrastructure;
//using BlockChain.Data;

//namespace BlockChain
//{
//	public class AddTx
//	{
//		private readonly BlockChain _BlockChain;
//		private Types.Transaction _Tx;
//		private byte[] _TxHash;
//		private TransactionContext _DbTx;

//		public AddTx(
//			BlockChain blockChain, 
//       		TransactionContext dbTx,
//			byte[] txHash,
//			Types.Transaction tx
//		)
//		{
//			_BlockChain = blockChain;
//			_TxHash = txHash;
//			_Tx = tx;
//			_DbTx = dbTx;
//		}

//		public bool Start(/*List<QueueAction> queuedActions, */bool isOrphan = false)
//		{
//			if (!IsValid() || IsInMempool() || IsInTxStore())
//			{
//				BlockChainTrace.Information("not valid/in mempool/in txstore");
//				return false;
//			}

//			if (IsMempoolContainsSpendingInput())
//			{
//				BlockChainTrace.Information("Mempool contains spending input");
//				return false;
//			}

//			TransactionValidation.PointedTransaction pointedTransaction = null;

//			if (_BlockChain.IsOrphanTx(_DbTx, _Tx, out pointedTransaction))
//			{
//				BlockChainTrace.Information("Added as orphan");
//				_BlockChain.TxMempool.AddOrphan(_TxHash, _Tx);
//				return true;
//			}

//			//TODO: 5. For each input, if the referenced transaction is coinbase, reject if it has fewer than COINBASE_MATURITY confirmations.

//			if (!_BlockChain.IsValidTransaction(_DbTx, pointedTransaction))
//			{
//				BlockChainTrace.Information("invalid inputs");
//				return false;
//			}

//			//TODO: 7. Apply fee rules. If fails, reject
//			//TODO: 8. Validate each input. If fails, reject

//			BlockChainTrace.Information("Transaction added to mempool");
//			_BlockChain.TxMempool.Add(_TxHash, pointedTransaction);

//			new MessageAction(new NewTxMessage(pointedTransaction)).Publish();
//			//queuedActions.Add(new MessageAction(new NewTxMessage(pointedTransaction)));

//			foreach (var transaction in _BlockChain.TxMempool.GetOrphansOf(_TxHash))
//			{
//				new HandleOrphanTxAction(_Tx).Publish();
//				//queuedActions.Add(new HandleOrphanTxAction(_Tx));

//				//BlockChainTrace.Information("Start with orphan tx");
//				//new AddTx(
//				//	_BlockChain,
//				//	_DbTx,
//				//	_Tx,
//				//	_DoActions
//				//).Start();
//			}

//			return true;
//		}

//		private bool IsValid()
//		{
//			return true;
//		}

//		private bool IsInMempool()
//		{
//			var result = _BlockChain.TxMempool.ContainsKey(_TxHash);

//			BlockChainTrace.Information($"IsInMempool = {result}");

//			return result;
//		}

//		private bool IsInTxStore()
//		{
//			var result = _BlockChain.BlockStore.TxStore.ContainsKey(_DbTx, _TxHash);

//			BlockChainTrace.Information($"IsInTxStore = {result}");

//			return result;
//		}

//		private bool IsMempoolContainsSpendingInput()
//		{
//			return _BlockChain.TxMempool.ContainsInputs(_Tx);
//		}
//	}
//}