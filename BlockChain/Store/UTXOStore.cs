using System;
using Store;
using Consensus;
using MsgPack;
using MsgPack.Serialization;

namespace BlockChain.Store
{
	public class UTXOStore : ConsensusTypeStore<Types.Outpoint, Types.Output>
	{
		public UTXOStore() : base("utxo")
		{
		}

		public void Remove(TransactionContext dbTx, byte[] txHash, uint index)
		{
			Remove(dbTx, new Types.Outpoint(txHash, index));
		}

		public void Put(TransactionContext dbTx, byte[] txHash, uint index, Types.Output value) //TODO: use Keyed?
		{
			Put(dbTx, new Types.Outpoint(txHash, index), value);
		}

		public Keyed<Types.Outpoint, Types.Output> Get(TransactionContext dbTx, byte[] txHash, uint index)
		{
			return Get(dbTx, new Types.Outpoint(txHash, index));
		}

		public bool ContainsKey(TransactionContext dbTx, byte[] txHash, uint index)
		{
			return ContainsKey(dbTx, new Types.Outpoint(txHash, index));
		}
	}

	//public class UTXOMiroringCache
	//{
	//	private readonly UTXOStore _UTXOStore;

	//	public UTXOMiroringCache() 
	//	{
	//		_UTXOStore = new UTXOStore();
	//	}
	
	//	private void Put() {
	//	}

	//	private void Get() {
	//	}


	//	private class UTXOStore : Store<Types.Output>
	//	{
	//		public UTXOStore() : base("utxo") { }

	//		protected override StoredItem<Types.Output> Wrap(Types.Output item)
	//		{
	//			var data = Merkle.serialize<Types.Output>(item);
	//			var key = Merkle.outputHasher.Invoke(item);

	//			return new StoredItem<Types.Output>(item, key, data);
	//		}

	//		protected override Types.Output FromBytes(byte[] data, byte[] key)
	//		{
	//			//TODO: encap unpacking in Consensus, so referencing MsgPack would become unnecessary 
	//			return Serialization.context.GetSerializer<Types.Output>().UnpackSingleObject(data);
	//		}
	//	}
	//}
}