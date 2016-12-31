using NUnit.Framework;
using System;
using BlockChain.Store;
using Store;
using System.Collections.Generic;
using System.Linq;
using Consensus;

namespace BlockChain.Tests
{
	[TestFixture()]
	public class BlockChainAddBlockOperationTests : TestBase
	{
		[Test()]
		public void ShouldAddBlock()
		{
			var p = new TestTransactionPool();

			p.Add("t1", 1);
			p.Add("t2", 0);
		//	p.Spend("t2", "t1", 0);

			p.Render();

			var genesisBlock = new TestBlock(p.TakeOut("t1").Value);
			genesisBlock.Render();
			var blockPool = new TestBlockBlockChainExpectationPool();
			blockPool.GenesisBlockHash = genesisBlock.Value.Key;

			blockPool.Add("genesis", genesisBlock, BlockChainAddBlockOperation.Result.Added);
			blockPool.Add("block1", new TestBlock(p.TakeOut("t2").Value), BlockChainAddBlockOperation.Result.Added, "genesis");
		//	blockPool.Render();

			ScenarioAssertion(blockPool);
		}

		[Test()]
		public void ShouldAddBlockAndHandleOrpand()
		{
			var p = new TestTransactionPool();

			p.Add("t1", 1);
			p.Add("t2", 0);
			p.Spend("t2", "t1", 0);

			p.Render();

			var genesisBlock = new TestBlock(p.TakeOut("t1").Value);
			genesisBlock.Render();

			var block1 = new TestBlock(p.TakeOut("t2").Value);
			block1.Parent = genesisBlock;
			block1.Render();

			var mempool = new TxMempool();
			var txStore = new TxStore();
			var utxoStore = new UTXOStore();
			var chainTip = new ChainTip();

			var mainBlockStore = new MainBlockStore();
			var branchBlockStore = new BranchBlockStore();
			var orphanBlockStore = new OrphanBlockStore();
			//	var genesisBlockStore = new GenesisBlockStore();

			using (TestDBContext dbContext = new TestDBContext())
			{
				using (TransactionContext transactionContext = dbContext.GetTransactionContext())
				{
					BlockChainAddBlockOperation.Result result1 = new BlockChainAddBlockOperation(
						transactionContext, block1.Value, mainBlockStore, branchBlockStore, orphanBlockStore, mempool, txStore, utxoStore, chainTip, genesisBlock.Value.Key
					).Start();

					BlockChainAddBlockOperation.Result result2 = new BlockChainAddBlockOperation(
						transactionContext, genesisBlock.Value, mainBlockStore, branchBlockStore, orphanBlockStore, mempool, txStore, utxoStore, chainTip, genesisBlock.Value.Key
					).Start();

					var expectedBlocks = new List<Types.Block>();

					foreach (var block in mainBlockStore.All(transactionContext))
					{
						expectedBlocks.Remove(block.Value);
					}

					Assert.That(expectedBlocks, Is.Empty);
				}
			}
		}

		[Test()]
		public void ShouldAddBlockWithSpendingTransaction()
		{
			var p = new TestTransactionPool();

			p.Add("t1", 1);
			p.Add("t2", 0);
			p.Spend("t2", "t1", 0);

			p.Render();

			var genesisBlock = new TestBlock(p.TakeOut("t1").Value);
			genesisBlock.Render();
			var blockPool = new TestBlockBlockChainExpectationPool();
			blockPool.GenesisBlockHash = genesisBlock.Value.Key;

			blockPool.Add("genesis", genesisBlock, BlockChainAddBlockOperation.Result.Added);
			blockPool.Add("block1", new TestBlock(p.TakeOut("t2").Value), BlockChainAddBlockOperation.Result.Added, "genesis");
			//	blockPool.Render();

			ScenarioAssertion(blockPool);
		}


		//[Test()]
		//public void ShouldBeAdddedAsOrphan()
		//{
		//	var blockPool = new TestBlockBlockChainExpectationPool();

		//	var p1 = new TestTransactionPool();
		//	p1.Add("t1", 0);

		//	blockPool.Blocks["block1"] = new TestBlock(p1);
		//	blockPool.Expectations["block1"] = BlockChainAddBlockOperation.Result.AddedOrphan;

		//	ScenarioAssertion(blockPool);
		//}

		private void ScenarioAssertion(
			TestBlockBlockChainExpectationPool p, 
			Action<TxMempool, TxStore, UTXOStore, TransactionContext> preAction = null,
			Action<TxMempool, TxStore, UTXOStore, TransactionContext> postAction = null
		)
		{
//			var genesisBlock = Util.GetGenesisBlock();
			var mempool = new TxMempool();
			var txStore = new TxStore();
			var utxoStore = new UTXOStore();
			var chainTip = new ChainTip();

			var mainBlockStore = new MainBlockStore();
			var branchBlockStore = new BranchBlockStore();
			var orphanBlockStore = new OrphanBlockStore();
		//	var genesisBlockStore = new GenesisBlockStore();

			using (TestDBContext dbContext = new TestDBContext())
			{
				using (TransactionContext transactionContext = dbContext.GetTransactionContext())
				{
					if (preAction != null)
					{
						preAction(mempool, txStore, utxoStore, transactionContext);
					}

					p.Render();

					foreach (var key in p.List)
					{
						BlockChainAddBlockOperation.Result t = p.Expectations[key];

						BlockChainAddBlockOperation.Result result = new BlockChainAddBlockOperation(
							transactionContext, p.Blocks[key].Value, mainBlockStore, branchBlockStore, orphanBlockStore, mempool, txStore, utxoStore, chainTip, p.GenesisBlockHash
						).Start();

						Assert.AreEqual(t, result, "Assertion for tag: " + key);
					}

					if (postAction != null)
					{
						postAction(mempool, txStore, utxoStore, transactionContext);
					}
				}
			}
		}
	}
}