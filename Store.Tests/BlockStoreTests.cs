using System;
using Consensus;
using System.Collections.Generic;
using BlockChain.Store;
using BlockChain.Data;
using BlockChain.Database;
using NUnit.Framework;
using System.IO;

namespace BlockChain.Tests
{
	public class BlockStoreTests : TestBase
	{
		[Test()]
		public void CanStoreBlocks()
		{
			String dbName = "test-" + new Random().Next(0, 1000);

			BlockStore blockStore = new BlockStore();

			List<Types.Block> blocks = new List<Types.Block>();

			for (int i = 0; i < 10; i++)
			{
				Types.Block newBlock = Util.GetBlock(null, new Random().Next(0, 1000));

				using (DBContext dbContext = new DBContext(dbName))
				{
					using (TransactionContext context = dbContext.GetTransactionContext())
					{
						blockStore.Put(context, newBlock);
						context.Commit();
					}
				}

				blocks.Add(newBlock);
			}

			using (DBContext dbContext = new DBContext(dbName))
			{
				using (TransactionContext context = dbContext.GetTransactionContext())
				{
					foreach (Types.Block expected in blocks)
					{
						byte[] key = Merkle.blockHasher.Invoke(expected);

						Assert.IsTrue(blockStore.ContainsKey(context, key), "should contain key of added object");

						Types.Block found = blockStore.Get(context, key);

						Assert.AreEqual(expected, found, "match expected/found block");
					}
				}
			}

			Directory.Delete(dbName, true);
		}
	}
}