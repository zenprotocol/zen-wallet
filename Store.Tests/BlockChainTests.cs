using System;
using BlockChain.Store;
using NUnit.Framework;
using Store;

namespace BlockChain.Tests
{
	[TestFixture()]
	public class BlockChainTests : TestBase
	{
		[Test()]
		public void CanFindOrphansByParent()
		{
			var store = new OrphanBlockStore();

			var p1 = new TestTransactionPool();
			p1.Add("t1", 0);
			p1.Add("t2", 0);
			p1.Add("t3", 0);
			p1.Render();


			var block1 = new TestBlock(p1.TakeOut("t1").Value);
			var block2 = new TestBlock(p1.TakeOut("t2").Value);
			var block3 = new TestBlock(p1.TakeOut("t3").Value);

			block2.Parent = block1;
			block3.Parent = block1;

			block1.Render();
			block2.Render();
			block3.Render();

			using (TestDBContext dbContext = new TestDBContext())
			{
				using (TransactionContext transactionContext = dbContext.GetTransactionContext())
				{
					store.Put(transactionContext, block2.Value);
					store.Put(transactionContext, block3.Value);

					var result = store.GetOrphansOf(transactionContext, block1.Value);

//					CollectionAssert.Contains(result, block2.Value);
//					CollectionAssert.Contains(result, block3.Value);
				}
			}
		}

		[Test()]
		public void Test1()
		{
			var blockPool = new TestBlockBlockChainExpectationPool();

			var p1 = new TestTransactionPool();
			p1.Add("t1", 0);
	//		p1.Add("t2", 0);

			blockPool.Blocks["block1"] = new TestBlock(p1.TakeOut("t1").Value);
			blockPool.Expectations["block1"] = BlockChainAddBlockOperation.Result.AddedOrphan;

			ScenarioAssertion(blockPool);
		}

		private void ScenarioAssertion(
			TestBlockBlockChainExpectationPool p,
			Action<BlockChain> preAction = null,
			Action<BlockChain> postAction = null
		) {
			using (var testBlockChain = new TestBlockChain())
			{
				if (preAction != null)
				{
					preAction(testBlockChain.BlockChain);
				}

				p.Render();

				foreach (var item in p.Blocks)
				{
					BlockChainAddBlockOperation.Result t = p.Expectations[item.Key];

					BlockChainAddBlockOperation.Result result = testBlockChain.BlockChain.HandleNewBlock(
						item.Value.Value.Value //yeah!
					);

					Assert.AreEqual(t, result, "Assertion for tag: " + item.Key);
				}

				if (postAction != null)
				{
					postAction(testBlockChain.BlockChain);
				}
			}
		}
	}
}
