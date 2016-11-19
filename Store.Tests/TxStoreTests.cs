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
	public class TxStoreTests : TestBase
	{
		[Test()]
		public void CanStoreTx()
		{
			using (TestDBContext<TxStore> dbContext = new TestDBContext<TxStore>())
			{
				using (TransactionContext transactionContext = dbContext.GetTransactionContext())
				{
					var p = new TestTransactionPool();

					p.Add("test1", 1);
					p.Add("test2", 0);
					p.Spend("test2", "test1", 0);
					p.Render();

					var txStore = new TxStore();

					txStore.Put(transactionContext, p["test1"].Value);
					txStore.Put(transactionContext, p["test2"].Value);

					Assert.IsTrue(txStore.ContainsKey(transactionContext, p["test1"].Key));
					Assert.IsTrue(txStore.ContainsKey(transactionContext, p["test2"].Key));

					var test1 = txStore.Get(transactionContext, p["test1"].Key);
					var test2 = txStore.Get(transactionContext, p["test2"].Key);
				}
			}
		}
	}
}
