//using System;
//using Consensus;
//using System.Linq;
//using Store;
//using Infrastructure;
//using System.Collections.Generic;

//namespace BlockChain.Store
//{
//	public class TxStore : ConsensusTypeStore<Types.Transaction>
//	{
//		private const string TRANSACTIONS = "bk-txs";

//		public TxStore() : base("tx")
//		{
//		}

//		public new void Put(TransactionContext transactionContext, Keyed<Types.Transaction> item, byte[] blockHeader)
//		{
//			Put(transactionContext, item);

//			//transactions
//			var transactions = new HashSet<byte[]>();

//			transactions.Add(item.Key);

//			transactionContext.Transaction.InsertHashSet<byte[], byte[]>(
//				CHILDREN,
//				item.Key,
//				children,
//				0,
//				false
//			);

//			transactionContext.Transaction.Insert<byte[], byte[]>(TRANSACTIONS, item.Key, Merkle.transactionHasher.Invoke(tx));
//		}

//		public IEnumerable<Types.Transaction> All(TransactionContext transactionContext, byte[] blockHeader)
//		{
//			foreach (var row in transactionContext.Transaction.SelectForward<byte[], byte[]>(TRANSACTIONS))
//			{
//				yield return Get(transactionContext, row.Value

//						var tx = _BlockChain.TxStore.Get(_DbTx, txHash);

//				row.Value;
//			}
//		}



//		//	for (int i = 0; i < item.Value.outputs.Length; i++)
//		//	{
//		//		var output = item.Value.outputs[i];
//		//		var txHash = Merkle.transactionHasher.Invoke(item.Value);

//		//		byte[] outputKey = new byte[txHash.Length + 1];
//		//		txHash.CopyTo(outputKey, 1);
//		//		outputKey[txHash.Length] = (byte)i;

//		//		_UTXOStore.Put(transactionContext, new Keyed<Types.Output>(GetOutputKey(item.Value, (uint)i), item.Value.outputs[i]));
//		//	}
//		//}

//		//public bool UTXOContains(TransactionContext transactionContext, Types.Outpoint outpoint)
//		//{
//		//	if (!ContainsKey(transactionContext, outpoint.txHash))
//		//	{
//		//		throw new Exception("Tx not found");
//		//	}

//		//	var tx = Get(transactionContext, outpoint.txHash);
//		//	return _UTXOStore.ContainsKey(transactionContext, GetOutputKey(tx.Value, outpoint.index));
//		//}

//		//private byte[] GetOutputKey(Types.Transaction transaction, uint index)
//		//{
//		//	var output = transaction.outputs[(int)index];
//		//	var txHash = Merkle.transactionHasher.Invoke(transaction);

//		//	byte[] outputKey = new byte[txHash.Length + 1];
//		//	txHash.CopyTo(outputKey, 1);
//		//	outputKey[txHash.Length] = (byte)index;

//		//	return outputKey;
//		//}
//	}
//}