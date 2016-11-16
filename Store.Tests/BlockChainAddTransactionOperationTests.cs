using NUnit.Framework;
using System;
using Consensus;

namespace Store.Tests
{
	[TestFixture()]
	public class BlockChainAddTransactionOperationTests : TestBase
	{
		[Test()]
		public void ShouldRejectNewTx_DueToExistingMempoolTx()
		{
			Types.Transaction transaction = GetNewTransaction(1);

			TxMempool mempool = new TxMempool();
			TxStore txStore = new TxStore();

			mempool.Add(new Keyed<Types.Transaction>(transaction, Merkle.transactionHasher.Invoke(transaction)));
			Assert.IsTrue(mempool.ContainsKey(Merkle.transactionHasher.Invoke(transaction)));

			BlockChainAddTransactionOperation.Result result;

			using (TestDBContext<TxStore> dbContext = new TestDBContext<TxStore>())
			{
				using (TransactionContext transactionContext = dbContext.GetTransactionContext())
				{
					result = new BlockChainAddTransactionOperation(
						transactionContext, transaction, mempool, txStore
					).Start();
				}
			}

			Assert.AreEqual(BlockChainAddTransactionOperation.Result.Rejected, result);
		}
	}
}
