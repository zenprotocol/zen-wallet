using System;
using Consensus;
using Store;

namespace BlockChain.Store
{
	public class ConsensusTypeStore<T> : Store<T> where T : class
	{
		public ConsensusTypeStore(String tableName) : base(tableName)
		{
		}

		protected override T Unpack(byte[] data, byte[] key)
		{
			return Serialization.context.GetSerializer<T>().UnpackSingleObject(data);
		}

		protected override byte[] Pack(T item)
		{
			return Merkle.serialize<T>(item);
		}
	}
}
