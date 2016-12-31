using System;
using Consensus;
using System.Linq;
using Store;
using Infrastructure;

namespace BlockChain.Store
{
	public class TxStore : ConsensusTypeStore<Types.Transaction>
	{
		public class AddedMessage { public Keyed<Types.Transaction> Transaction { get; set; } }

		private static MessageProducer<AddedMessage> _Producer = MessageProducer<AddedMessage>.Instance;

		//	private UTXOStore _UTXOStore;

		public TxStore() : base("tx")
		{
	//		_UTXOStore = new UTXOStore();
		}


		public new void Put(TransactionContext transactionContext, Keyed<Types.Transaction> item)
		{
			base.Put(transactionContext, item);

			_Producer.PushMessage(new AddedMessage() { Transaction = item });
		}

		//	for (int i = 0; i < item.Value.outputs.Length; i++)
		//	{
		//		var output = item.Value.outputs[i];
		//		var txHash = Merkle.transactionHasher.Invoke(item.Value);

		//		byte[] outputKey = new byte[txHash.Length + 1];
		//		txHash.CopyTo(outputKey, 1);
		//		outputKey[txHash.Length] = (byte)i;

		//		_UTXOStore.Put(transactionContext, new Keyed<Types.Output>(GetOutputKey(item.Value, (uint)i), item.Value.outputs[i]));
		//	}
		//}

		//public bool UTXOContains(TransactionContext transactionContext, Types.Outpoint outpoint)
		//{
		//	if (!ContainsKey(transactionContext, outpoint.txHash))
		//	{
		//		throw new Exception("Tx not found");
		//	}

		//	var tx = Get(transactionContext, outpoint.txHash);
		//	return _UTXOStore.ContainsKey(transactionContext, GetOutputKey(tx.Value, outpoint.index));
		//}

		//private byte[] GetOutputKey(Types.Transaction transaction, uint index)
		//{
		//	var output = transaction.outputs[(int)index];
		//	var txHash = Merkle.transactionHasher.Invoke(transaction);

		//	byte[] outputKey = new byte[txHash.Length + 1];
		//	txHash.CopyTo(outputKey, 1);
		//	outputKey[txHash.Length] = (byte)index;

		//	return outputKey;
		//}
	}
}