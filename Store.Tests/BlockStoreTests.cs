using System;
using Consensus;
using System.Collections.Generic;
using BlockChain.Store;
using NUnit.Framework;
using System.IO;
using Store;

namespace BlockChain.Tests
{
	public class BlockStoreTests : TestBase
	{
		[Test()]
		public void CanStoreBlocks()
		{
			String dbName = "test-" + new Random().Next(0, 1000);

			MainBlockStore blockStore = new MainBlockStore();

			List<Types.Block> blocks = new List<Types.Block>();

			for (int i = 0; i < 10; i++)
			{
				Types.Block newBlock = Util.GetBlock(null, new Random().Next(0, 1000));

				using (DBContext dbContext = new DBContext(dbName))
				{
					using (TransactionContext context = dbContext.GetTransactionContext())
					{
						blockStore.Put(context, new Keyed<Types.Block>(Merkle.blockHeaderHasher.Invoke(newBlock.header), newBlock));
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
						byte[] key = Merkle.blockHeaderHasher.Invoke(expected.header);

						Assert.IsTrue(blockStore.ContainsKey(context, key), "should contain key of added object");

						Types.Block found = blockStore.Get(context, key).Value;

						Assert.AreEqual(expected, found, "match expected/found block");
					}
				}
			}

			Directory.Delete(dbName, true);
		}

		[Test()]
		public void CanStoreUTXO()
		{
			using (TestDBContext dbContext = new TestDBContext())
			{
				using (var transactionContext = dbContext.GetTransactionContext())
				{
					var output = Consensus.Tests.pkoutput;
					var key = Consensus.Merkle.outputHasher.Invoke(output);

					var keyedUTXO = new Keyed<Consensus.Types.Output>(key, output);

					var utxoStore = new UTXOStore();
					utxoStore.Put(transactionContext, keyedUTXO);

					Assert.IsTrue(utxoStore.ContainsKey(transactionContext, key));

					var fromDb = utxoStore.Get(transactionContext, key);

					Assert.That(fromDb.Key, Is.SameAs(key));
					Assert.That(fromDb.Value, Is.EqualTo(output));
				}
			}
		}

		//[Test()]
		//public void CanStoreTx()
		//{
		//	using (TestDBContext dbContext = new TestDBContext())
		//	{
		//		using (var transactionContext = dbContext.GetTransactionContext())
		//		{
		//			var p = new TestTransactionPool();

		//			p.Add("test1", 1);
		//			p.Add("test2", 0);
		//			p.Spend("test2", "test1", 0);
		//			p.Render();

		//			var txStore = new TxStore();

		//			txStore.Put(transactionContext, p["test1"]);
		//			txStore.Put(transactionContext, p["test2"]);

		//			Assert.IsTrue(txStore.ContainsKey(transactionContext, p["test1"].Key));
		//			Assert.IsTrue(txStore.ContainsKey(transactionContext, p["test2"].Key));

		//			var test1 = txStore.Get(transactionContext, p["test1"].Key);
		//			var test2 = txStore.Get(transactionContext, p["test2"].Key);
		//		}
		//	}
		//}
	}
}