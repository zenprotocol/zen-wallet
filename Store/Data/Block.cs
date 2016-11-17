////using System;
////using System.Collections.Generic;
////using Consensus;

////namespace BlockChain
////{
////	public class Block
////	{
////		public BlockDifficultyHeader Header { get; set; }
////		public IEnumerable<byte[]> TXIDs { get; set; }
////		public Double Difficulty { get; set; }

////		public Block(BlockDifficultyHeader header, IEnumerable<byte[]> txIDs, Double difficulty)
////		{
////			Header = header;
////			txIDs = TXIDs;
////			Difficulty = difficulty;
////		}
////	}
////}


//using System;
//using Consensus;
//using System.Linq;

//namespace BlockChain
//{
//	public class BlockDifficulty
//	{
//		public byte[] BlockId { get; set; }
//		public Double Difficulty { get; set; }
//	}

//	public class BlockDifficultyStore : Store<BlockDifficulty>
//	{
//		public BlockDifficultyStore() : base("bk-difficulty")
//		{
//		}

//		protected override StoredItem<BlockDifficulty> Wrap(BlockDifficulty item)
//		{
//			var data = item.Difficulty;
//			var key = item.BlockId;

//			return new StoredItem<BlockDifficulty>(item, key, data);
//		}

//		protected override BlockDifficulty FromBytes(byte[] data, byte[] key)
//		{
//			//TODO: encap unpacking in Consensus, so referencing MsgPack would becode unnecessary 
//			return Serialization.context.GetSerializer<BlockDifficulty>().UnpackSingleObject(data);
//		}
//	}
//}