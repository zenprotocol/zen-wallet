using System;
using Consensus;

namespace Store
{
	public class UnspentOutputStore : Store<Types.Output>
	{
		public UnspentOutputStore(string dbName) : base(dbName, "utxo")
		{
		}

		protected override Types.Output FromBytes(byte[] data, byte[] key)
		{
			throw new NotImplementedException();
		}

		protected override StoredItem<Types.Output> Wrap(Types.Output item)
		{
			throw new NotImplementedException();
		}
	}
}
