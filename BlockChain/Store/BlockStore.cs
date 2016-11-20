using System;
using Consensus;
using System.Linq;
using Store;
using BlockChain.Data;

namespace BlockChain.Store
{
	public class BlockStore : Store<Types.Block>
	{
		public BlockStore() : base("bk")
		{
		}

		protected override StoredItem<Types.Block> Wrap(Types.Block item)
		{
			var data = Merkle.serialize<Types.Block>(item);
			var key = Merkle.blockHasher.Invoke(item); //TODO: id should be hash of block header, blockHasher may be redundant and wrong to use here


			return new StoredItem<Types.Block>(key, item, data);
		}

		protected override Types.Block FromBytes(byte[] data, byte[] key)
		{
			//TODO: encap unpacking in Consensus, so referencing MsgPack would become unnecessary 
			return Serialization.context.GetSerializer<Types.Block>().UnpackSingleObject(data);
		}
	}
}