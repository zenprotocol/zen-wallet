using System;
using Store;
using Consensus;

namespace Wallet.Store
{
	public class TxStore : EnumerableStore<Types.Transaction>
	{
		public TxStore() : base("tx")
		{
		}

		protected override StoredItem<Types.Transaction> Wrap(Types.Transaction item)
		{
			var data = Merkle.serialize<Types.Transaction>(item);
			var key = Merkle.transactionHasher.Invoke(item);

			return new StoredItem<Types.Transaction>(key, item, data);
		}

		protected override Types.Transaction FromBytes(byte[] data, byte[] key)
		{
			//TODO: encap unpacking in Consensus, so referencing MsgPack would become unnecessary 
			return Serialization.context.GetSerializer<Types.Transaction>().UnpackSingleObject(data);
		}
	}
}
