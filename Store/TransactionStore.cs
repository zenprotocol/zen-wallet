using System;
using Consensus;
using System.IO;
using System.Linq;

namespace Store
{
	public class TransactionStore : Store<Types.Transaction>
	{
		public TransactionStore(string dbName, string tableName) : base(dbName, "tx-" + tableName)
		{
		}

		protected override StoredItem<Types.Transaction> Wrap(Types.Transaction item)
		{
			var data = Merkle.serialize<Types.Transaction>(item);
			var key = Merkle.transactionHasher.Invoke(item);

			return new StoredItem<Types.Transaction>(item, key, data);
		}

		protected override Types.Transaction FromBytes(byte[] data, byte[] key)
		{
			//TODO: encap unpacking in Consensus, so referencing MsgPack would becode unnecessary 
			return Serialization.context.GetSerializer<Types.Transaction>().UnpackSingleObject(data);
		}
	}
}