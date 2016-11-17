using NUnit.Framework;
using System;
using Consensus;
using System.Collections.Generic;
using System.Linq;
using Microsoft.FSharp.Collections;
using System.IO;
using BlockChain.Database;
using BlockChain.Store;

namespace BlockChain.Tests
{
	[TestFixture()]
	public class BlockChainTests
	{
		[Test()]
		public void CanStoreBlocks()
		{
			String dbName = "test-" + new Random().Next(0, 1000);

			List<Types.Block> blocks = new List<Types.Block>();

			for (int i = 0; i < 10; i++)
			{
				Types.Block newBlock = Util.GetBlock(null, new Random().Next(0, 1000));

				using (BlockChain blockChain = new BlockChain(dbName))
				{
					blockChain.HandleNewValueBlock(newBlock);
				}

				blocks.Add(newBlock);
			}


			BlockStore blockStore = new BlockStore();

			using (DBContext dbContext = new DBContext(dbName))
			{
				using (TransactionContext context = dbContext.GetTransactionContext())
				{
					foreach (Types.Block expected in blocks)
					{
						byte[] key = Merkle.blockHasher.Invoke(expected);

						Assert.IsTrue(blockStore.ContainsKey(context, key));

						Types.Block found = blockStore.Get(context, key);

                      	Assert.AreEqual(expected, found, "match expected/found block");
					}
				}
			}

			Directory.Delete(dbName, true);
		}

		[Test()]
		public void CanStoreBlockDifficulty()
		{
			String dbName = "test-" + new Random().Next(0, 1000);

			var random = new Random();

			Double difficultyAgg = 0;

			List<Tuple<Types.Block, Double>> blocks = new List<Tuple<Types.Block, Double>>();

			Types.Block lastBlock = null;

			for (int i = 0; i < 10; i++)
			{
				Double difficultyNew = random.Next(0, int.MaxValue);
				Types.Block newBlock = Util.GetBlock(lastBlock, difficultyNew);
				lastBlock = newBlock;

				using (BlockChain blockChain = new BlockChain(dbName))
				{
					blockChain.HandleNewValueBlock(newBlock);
				}

				difficultyAgg += difficultyNew;

				blocks.Add(new Tuple<Types.Block, Double>(newBlock, difficultyAgg));
			}

			using (DBContext dbContext = new DBContext(dbName))
			{
				var BlockDifficultyTable = new BlockDifficultyTable();

				using (TransactionContext context = dbContext.GetTransactionContext())
				{
					foreach (Tuple<Types.Block, Double> blockDifficultyPair in blocks)
					{
						byte[] key = Merkle.blockHasher.Invoke(blockDifficultyPair.Item1);
						Double expected = blockDifficultyPair.Item2;
						Double found = BlockDifficultyTable.Context(context)[key];

						Assert.AreEqual(expected, found, "match expected/found block difficulty");
					}
				}
			}

			Directory.Delete(dbName, true);
		}

		//[Test()]
		//public void CanHandleNewValidBlock()
		//{
		//	Types.Block newBlock = GetBlock();

		//	String dbName = "test-" + new Random().Next(0, 1000);

		//	using (BlockChain blockChain = new BlockChain(dbName))
		//	{
		//		blockChain.HandleNewValueBlock(newBlock/*, 1*/);
		//	}

		//	Directory.Delete(dbName, true);
		//}
	}
}