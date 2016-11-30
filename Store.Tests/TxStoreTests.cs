using NUnit.Framework;
using BlockChain.Store;

namespace BlockChain.Tests
{
	[TestFixture()]
	public class TxStoreTests : TestBase
	{
		[Test()]
		public void CanStoreTx()
		{
			using (TestDBContext<TxStore> dbContext = new TestDBContext<TxStore>())
			{
				using (var transactionContext = dbContext.GetTransactionContext())
				{
					var p = new TestTransactionPool();

					p.Add("test1", 1);
					p.Add("test2", 0);
					p.Spend("test2", "test1", 0);
					p.Render();

					var txStore = new TxStore();

					txStore.Put(transactionContext, p["test1"]);
					txStore.Put(transactionContext, p["test2"]);

					Assert.IsTrue(txStore.ContainsKey(transactionContext, p["test1"].Key));
					Assert.IsTrue(txStore.ContainsKey(transactionContext, p["test2"].Key));

					var test1 = txStore.Get(transactionContext, p["test1"].Key);
					var test2 = txStore.Get(transactionContext, p["test2"].Key);
				}
			}
		}
	}
}
