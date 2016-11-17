using System;
using Consensus;
using System.Linq;

namespace Store
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


			return new StoredItem<Types.Block>(item, key, data);
		}

		protected override Types.Block FromBytes(byte[] data, byte[] key)
		{
			//TODO: encap unpacking in Consensus, so referencing MsgPack would becode unnecessary 
			return Serialization.context.GetSerializer<Types.Block>().UnpackSingleObject(data);
		}
	}
}