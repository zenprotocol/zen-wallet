using NUnit.Framework;
using System;
using BlockChain.Store;
using Store;

namespace BlockChain.Tests
{
	[TestFixture()]
	public class BlockChainAddTransactionOperationTests : TestBase
	{
		[Test()]
		public void CanRedeemOrphand()
		{
			var p = new TestTransactionBlockChainExpectationPool();

			p.Add("test1", 1, BlockChainAddTransactionOperation.Result.Added);
			p.Add("test2", 0, BlockChainAddTransactionOperation.Result.AddedOrphan);
			p.Spend("test2", "test1", 0);

			p.Render();
			var test1 = p.TakeOut("test1");

			ScenarioAssertion(p, postAction: (mempool, txstore, utxoStore, context) =>
			{
				var result = new BlockChainAddTransactionOperation(
					context, test1, mempool, txstore, utxoStore
				).Start();

				Assert.AreEqual(BlockChainAddTransactionOperation.Result.Added, result);
			});
		}

		[Test()]
		public void ShouldBeAddded()
		{
			var p = new TestTransactionBlockChainExpectationPool();

			p.Add("test1", 1, BlockChainAddTransactionOperation.Result.Added);
			p.Add("test2", 0, BlockChainAddTransactionOperation.Result.Added);
			p.Spend("test2", "test1", 0);

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

		[Test()]
		public void ShouldBeAddedAsOrphan()
		{
			var p = new TestTransactionBlockChainExpectationPool();

			p.Add("test1", 1, BlockChainAddTransactionOperation.Result.Added);
			p.Add("test2", 0, BlockChainAddTransactionOperation.Result.AddedOrphan);
			p.Spend("test2", "test1", 0);

			p.Render();
			p.TakeOut("test1");

			ScenarioAssertion(p);
		}

		[Test()]
		public void ShouldRejectNewTx_DueToSameTxIdExistsInMempool()
		{
			var p = new TestTransactionBlockChainExpectationPool();

			p.Add("test1", 0, BlockChainAddTransactionOperation.Result.Rejected);

			ScenarioAssertion(p, preAction: (mempool, txstore, utxoStore, context) => {
				p.Render();
				mempool.Add(p["test1"]);
			});
		}

		[Test()]
		public void ShouldRejectNewTx_DueToSameTxIdExistsInTxStore()
		{
			var p = new TestTransactionBlockChainExpectationPool();

			p.Add("test1", 0, BlockChainAddTransactionOperation.Result.Rejected);

			ScenarioAssertion(p, preAction: (mempool, txstore, utxoStore, context) =>
			{
				p.Render();
				txstore.Put(context, p["test1"]);
			});
		}

		[Test()]
		public void ShouldRejectNewTx_DueToMempoolContainsSpendingOutput()
		{
			var p = new TestTransactionBlockChainExpectationPool();

			p.Add("test1", 1, BlockChainAddTransactionOperation.Result.Added);
			p.Add("spend_in_mempool", 0, BlockChainAddTransactionOperation.Result.Added);
			p.Spend("spend_in_mempool", "test1", 0);
			p.Add("test2", 0, BlockChainAddTransactionOperation.Result.Rejected);
			p.Spend("test2", "test1", 0);

			p.Render();

			var spend_in_mempool = p.TakeOut("spend_in_mempool");

			ScenarioAssertion(p, preAction: (mempool, txstore, utxoStore, context) =>
			{
				mempool.Add(spend_in_mempool);
			});
		}

		[Test()]
		public void ShouldRejectNewTx_DueToReferencedOutputDoesNotExist_DueToMissingOutputIndex()
		{
			var p = new TestTransactionBlockChainExpectationPool();

			p.Add("test1", 1, BlockChainAddTransactionOperation.Result.Added);
			p.Add("test2", 0, BlockChainAddTransactionOperation.Result.Rejected);
			p.Spend("test2", "test1", 1);

			ScenarioAssertion(p);
		}

		[Test()]
		public void ShouldRejectNewTx_DueToReferencedOutputDoesNotExist_DueToOutputWasSpent()
		{
			var p = new TestTransactionBlockChainExpectationPool();

			p.Add("test1", 1, BlockChainAddTransactionOperation.Result.Added);
			p.Add("test2", 0);
			p.Spend("test2", "test1", 0);
			p.Add("test3", 0, BlockChainAddTransactionOperation.Result.Added);
			p.Spend("test3", "test1", 0);

			p.Render();
			var test1 = p.TakeOut("test1");
			var test3 = p.TakeOut("test3");

			ScenarioAssertion(p, preAction: (mempool, txstore, utxoStore, context) =>
			{
				txstore.Put(context, test1);

				var index = 0;
				var output = test1.Value.outputs[index];

				var txHash = Consensus.Merkle.transactionHasher.Invoke(test1.Value);

				//System.Buffer.BlockCopy(
				//System.Array.Copy(
				byte[] outputKey = new byte[txHash.Length + 1];
				txHash.CopyTo(outputKey, 0);
				outputKey[txHash.Length] = (byte)index;

				utxoStore.Put(context, new Keyed<Consensus.Types.Output>(outputKey, output));
			}, postAction: (mempool, txstore, utxoStore, context) =>
			{
				var result = new BlockChainAddTransactionOperation(
					context, test3, mempool, txstore, utxoStore
				).Start();

				Assert.AreEqual(BlockChainAddTransactionOperation.Result.Rejected, result, "test3");
			});
		}

		private void ScenarioAssertion(
			TestTransactionBlockChainExpectationPool p, 
			Action<TxMempool, TxStore, UTXOStore, TransactionContext> preAction = null,
			Action<TxMempool, TxStore, UTXOStore, TransactionContext> postAction = null
		)
		{
			var mempool = new TxMempool();
			var txStore = new TxStore();
			var utxoStore = new UTXOStore();

			using (TestDBContext dbContext = new TestDBContext())
			{
				using (TransactionContext transactionContext = dbContext.GetTransactionContext())
				{
					if (preAction != null)
					{
						preAction(mempool, txStore, utxoStore, transactionContext);
					}

					p.Render();

				 	foreach (var key in p.Keys)
					{
						TestTransactionBlockChainExpectation t = p.GetItem(key);

						BlockChainAddTransactionOperation.Result result = new BlockChainAddTransactionOperation(
							transactionContext, t.Value, mempool, txStore, utxoStore
						).Start();

						Assert.AreEqual(t.Result, result, "Assertion for tag: " + key);
					}

					if (postAction != null)
					{
						postAction(mempool, txStore, utxoStore, transactionContext);
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

			Assert.AreEqual(testTransactionPool["tx1"].Value.inputs[0].txHash, key1, "should reference previous transaction");
		}
	}
}