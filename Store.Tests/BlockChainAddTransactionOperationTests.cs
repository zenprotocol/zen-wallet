using NUnit.Framework;
using System;
using Consensus;
using BlockChain.Store;
using BlockChain.Data;
using BlockChain.Database;
using System.Collections.Generic;
using System.Linq;

namespace BlockChain.Tests
{
	[TestFixture()]
	public class BlockChainAddTransactionOperationTests : TestBase
	{
		[Test()]
		public void ShouldBeAddded()
		{
			var p = new TestTransactionBlockChainExpectationPool();

			p.Add("test1", 0, BlockChainAddTransactionOperation.Result.Added);
			p.Add("test2", 0, BlockChainAddTransactionOperation.Result.Added);

			ScenarioAssertion(p);
		}

		[Test()]
		public void ShouldBeRejected()
		{
			var p = new TestTransactionBlockChainExpectationPool();

			p.Add("test1", 0, BlockChainAddTransactionOperation.Result.Added);
			p.Add("test2", 0, BlockChainAddTransactionOperation.Result.Rejected);

			ScenarioAssertion(p);
		}

		//[Test()]
		//public void ShouldBeAddedAsOrphaned()
		//{
		//	TestTransactionBlockChainExpectationPool p = new TestTransactionBlockChainExpectationPool();

		//	p.Add("test1", 1, BlockChainAddTransactionOperation.Result.Added);
		//	p.Add("test2", 0, BlockChainAddTransactionOperation.Result.AddedOrphaned);
		//	p.Spend("test2", "test1", 1);

		//	ScenarioAssertion(p);
		//}


		[Test()]
		public void ShouldRejectNewTx_DueToSameTxIdExistsInMempool()
		{
			var p = new TestTransactionBlockChainExpectationPool();

			p.Add("test1", 0, BlockChainAddTransactionOperation.Result.Rejected);

			ScenarioAssertion(p, (mempool, txstore, context) => {
				TestTransactionPool p1 = new TestTransactionPool();
				p1.Add("mempool1", 0);
				mempool.Add(p1["mempool1"]);
			});
		}

		[Test()]
		public void ShouldRejectNewTx_DueToSameTxIdExistsInTxStore()
		{
			var p = new TestTransactionBlockChainExpectationPool();

			p.Add("test1", 0, BlockChainAddTransactionOperation.Result.Rejected);

			ScenarioAssertion(p, (mempool, txstore, context) =>
			{
				TestTransactionPool p1 = new TestTransactionPool();
				p1.Add("mempool1", 0);
				txstore.Put(context, p1["mempool1"].Value);
			});
		}

		[Test()]
		public void ShouldRejectNewTx_DueToMempoolContainsSpendingOutput()
		{
			var p = new TestTransactionBlockChainExpectationPool();

			p.Add("test1", 1, BlockChainAddTransactionOperation.Result.Added);
			p.Add("test2", 1, BlockChainAddTransactionOperation.Result.AddedOrphaned);
			p.Spend("test2", "test1", 0);
			p.Add("test3", 1, BlockChainAddTransactionOperation.Result.Rejected);
			p.Spend("test3", "test1", 0);

			ScenarioAssertion(p);
		}

		[Test()]
		public void ShouldRejectNewTx_DueToReferencedOutputDoesNotExist_DueToMissingOutputIndex()
		{
		}

		[Test()]
		public void ShouldRejectNewTx_DueToReferencedOutputDoesNotExist_DueToOutputWasSpent()
		{
		}

		private void ScenarioAssertion(TestTransactionBlockChainExpectationPool p, Action<TxMempool, TxStore, TransactionContext> preAction = null)
		{
			var mempool = new TxMempool();
			var txStore = new TxStore();

			using (TestDBContext<TxStore> dbContext = new TestDBContext<TxStore>())
			{
				using (TransactionContext transactionContext = dbContext.GetTransactionContext())
				{
					if (preAction != null)
					{
						preAction(mempool, txStore, transactionContext);
					}

					p.Render();

				 	foreach (var key in p.Keys)
					{
						TestTransactionBlockChainExpectation t = p.GetItem(key);

						BlockChainAddTransactionOperation.Result result = new BlockChainAddTransactionOperation(
							transactionContext, t.Value, mempool
						).Start();

						Assert.AreEqual(t.Result, result, "Assertion for tag: " + key);
					}
				}
			}
		}

		[Test()]
		public void TestTransactionPool()
		{
			var testTransactionPool = new TestTransactionPool();

			testTransactionPool.Add("base", 1);
			testTransactionPool.Add("tx1", 0);
			testTransactionPool.Spend("tx1", "base", 0);

			testTransactionPool.Render();

			byte[] key1 = testTransactionPool["base"].Key;

			Assert.AreEqual(testTransactionPool["tx1"].Value.inputs[0].txHash, key1, "should reference to previous transaction");
		}
	}
}