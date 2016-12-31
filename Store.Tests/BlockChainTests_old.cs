//using NUnit.Framework;
//using System;
//using Consensus;
//using System.Collections.Generic;
//using System.IO;
//using BlockChain.Store;
//using Store;
//using System.Linq;
//using Microsoft.FSharp.Collections;

//namespace BlockChain.Tests
//{
//	[TestFixture()]
//	public class BlockChainTests_old : TestBase
//	{
//		//[Test()]
//		//public void ShoudHandleBlock()
//		//{
//		//	WithBlockChains(2, blockChains =>
//		//	{
//		//		BlockChainAddTransactionOperation.Result result = new BlockChainAddTransactionOperation
//		//			transactionContext, t.Value, mempool, txStore, utxoStore
//		//		).Start();

//		//		var p = new TestTransactionBlockChainExpectationPool();

//		//		p.Add("test1", 1, BlockChainAddTransactionOperation.Result.Added);
//		//		p.Add("test2", 0, BlockChainAddTransactionOperation.Result.AddedOrphan);
//		//		p.Spend("test2", "test1", 0);

//		//		p.Render();
//		//		var test1 = p.TakeOut("test1");

//		//		ScenarioAssertion(p, postAction: (mempool, txstore, utxostore, context) =>
//		//		{
//		//			var result = new BlockChainAddTransactionOperation(
//		//				context, test1, mempool, txstore, utxostore
//		//			).Start();

//		//			Assert.AreEqual(BlockChainAddTransactionOperation.Result.Added, result);
//		//		});
//		//	});
//		//}

//		//private void ScenarioAssertion(
//		//	TestTransactionBlockChainExpectationPool p,
//		//	Action<TxMempool, TxStore, UTXOStore, TransactionContext> preAction = null,
//		//	Action<TxMempool, TxStore, UTXOStore, TransactionContext> postAction = null
//		//)
//		//{
//		//	var mempool = new TxMempool();
//		//	var txStore = new TxStore();
//		//	var utxoStore = new UTXOStore();

//		//	using (TestDBContext dbContext = new TestDBContext())
//		//	{
//		//		using (TransactionContext transactionContext = dbContext.GetTransactionContext())
//		//		{
//		//			if (preAction != null)
//		//			{
//		//				preAction(mempool, txStore, utxoStore, transactionContext);
//		//			}

//		//			p.Render();

//		//			foreach (var key in p.Keys)
//		//			{
//		//				TestTransactionBlockChainExpectation t = p.GetItem(key);

//		//				BlockChainAddTransactionOperation.Result result = new BlockChainAddTransactionOperation(
//		//					transactionContext, t.Value, mempool, txStore, utxoStore
//		//				).Start();

//		//				Assert.AreEqual(t.Result, result, "Assertion for tag: " + key);
//		//			}

//		//			if (postAction != null)
//		//			{
//		//				postAction(mempool, txStore, utxoStore, transactionContext);
//		//			}
//		//		}
//		//	}
//		//}

//		//[Test()]
//		//public void CanUseGenesisTransaction()
//		//{
//		//	String dbName = "test-" + new Random().Next(0, 1000);

//		//	using (BlockChain blockChain = new BlockChain(dbName))
//		//	{
//		//		blockChain._TxStore.Put(GetGenesisTransaction()); 
//		//	}
//		//}

//		[Test()]
//		public void ShoudRejectDuplicateBlock()
//		{
//			WithBlockChains(1, blockChain =>
//			{
//	//			Assert.That(blockChain[0].HandleNewBlock(Consensus.Tests.blk), Is.EqualTo(BlockChain.HandleNewBlockResult.AddedOrpan));
//	//			Assert.That(blockChain[0].HandleNewBlock(Consensus.Tests.blk), Is.EqualTo(BlockChain.HandleNewBlockResult.Rejected));
//			});
//		}

//		[Test()]
//		public void CanStoreBlocks()
//		{
//			List<Types.Block> blocks = new List<Types.Block>();

//			for (int i = 0; i < 10; i++)
//			{
//				Types.Block newBlock = Util.GetBlock(null, new Random().Next(0, 1000));

//				using (TestBlockChain testBlockChain = new TestBlockChain())
//				{
//					testBlockChain.BlockChain.HandleNewBlock(newBlock);
//				}

//				blocks.Add(newBlock);
//			}


//			MainBlockStore blockStore = new MainBlockStore();

//			using (DBContext dbContext = new DBContext(dbName))
//			{
//				using (TransactionContext context = dbContext.GetTransactionContext())
//				{
//					foreach (Types.Block expected in blocks)
//					{
//						byte[] key = Merkle.blockHasher.Invoke(expected);

//						Assert.IsTrue(blockStore.ContainsKey(context, key));

//						Types.Block found = blockStore.Get(context, key).Value;

//						Assert.AreEqual(expected, found, "match expected/found block");
//					}
//				}
//			}

//			Directory.Delete(dbName, true);
//		}

//		[Test()]
//		public void CanStoreBlockDifficulty()
//		{
//			String dbName = "test-" + new Random().Next(0, 1000);

//			var random = new Random();

//			Double difficultyAgg = 0;

//			var blocks = new List<Tuple<Types.Block, Double>>();

//			Types.Block lastBlock = null;

//			Console.WriteLine("Creating");

//			for (int i = 0; i < 10; i++)
//			{
//				Double difficultyNew = random.Next(0, 1000);
//				Types.Block newBlock = Util.GetBlock(lastBlock, difficultyNew);
//				lastBlock = newBlock;

//				using (TestBlockChain testBlockChain = new TestBlockChain(dbName))
//				{
//					Console.WriteLine("Handling block with difficulty: " + difficultyNew);

//					testBlockChain.BlockChain.HandleNewBlock(newBlock);
//				}

//				difficultyAgg += difficultyNew;

//				Console.WriteLine("Totoal difficulty: " + difficultyAgg);

//				blocks.Add(new Tuple<Types.Block, Double>(newBlock, difficultyAgg));
//			}

//			Console.WriteLine("Checking");

//			using (DBContext dbContext = new DBContext(dbName))
//			{
//				var BlockDifficultyTable = new BlockDifficultyTable();

//				using (TransactionContext context = dbContext.GetTransactionContext())
//				{
//					foreach (Tuple<Types.Block, Double> blockDifficultyPair in blocks)
//					{
//						byte[] key = Merkle.blockHasher.Invoke(blockDifficultyPair.Item1);
//						Double expected = blockDifficultyPair.Item2;
//						Double found = BlockDifficultyTable.Context(context)[key];

//						Assert.AreEqual(expected, found, "match expected/found block difficulty");
//						Console.WriteLine("check passed..");
//					}
//				}
//			}

//			Directory.Delete(dbName, true);
//		}

//		//[Test()]
//		//public void CanHandleNewValidBlock()
//		//{
//		//	Types.Block newBlock = GetBlock();

//		//	String dbName = "test-" + new Random().Next(0, 1000);

//		//	using (BlockChain blockChain = new BlockChain(dbName))
//		//	{
//		//		blockChain.HandleNewValueBlock(newBlock/*, 1*/);
//		//	}

//		//	Directory.Delete(dbName, true);
//		//}

//		//copied from elsewhere
//		private void WithBlockChains(int blockChains, Action<BlockChain[]> action)
//		{
//			var testBlockChains = new List<TestBlockChain>();

//			for (int i = 0; i < blockChains; i++)
//			{
//				String dbName = "test-" + new Random().Next(0, 1000);
//				testBlockChains.Add(new TestBlockChain(dbName));
//			}

//			action(testBlockChains.Select(t => t.BlockChain).ToArray());

//			foreach (var testBlockChain in testBlockChains)
//			{
//				testBlockChain.Dispose();
//			}
//		}

//       //copied from elsewhere
//		private class TestBlockChain : IDisposable
//		{
//			private byte[] genesisBlockHash = new byte[] { 0x01, 0x02 };

//			private readonly String _DbName;
//			public BlockChain BlockChain { get; private set; }

//			public TestBlockChain(String dbName)
//			{
//				_DbName = dbName;
//				BlockChain = new BlockChain(dbName, genesisBlockHash);
//			}

//			public TestBlockChain() : this("test-" + new Random().Next(0, 1000)) { }

//			public void Dispose()
//			{
//				BlockChain.Dispose();
//				Directory.Delete(_DbName, true);
//			}
//		}
//	}
//}