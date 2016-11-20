using NUnit.Framework;
using System;
using Consensus;
using System.Linq;
using BlockChain.Store;
using Store;

namespace BlockChain.Tests
{
	[TestFixture()]
	public class TxMempoolTests : TestBase
	{
		[Test()]
		public void ShouldContainKey()
		{
			Types.Transaction transaction = Util.GetNewTransaction(1);

			var mempool = new TxMempool();

			var keyedTransaction =
				new Keyed<Types.Transaction>(Merkle.transactionHasher.Invoke(transaction), transaction);

			mempool.Add(keyedTransaction);

			byte[] newKey = Merkle.transactionHasher.Invoke(transaction);

			Assert.IsTrue(Enumerable.SequenceEqual(newKey, keyedTransaction.Key));
			Assert.IsTrue(mempool.ContainsKey(keyedTransaction.Key));
			Assert.IsTrue(mempool.ContainsKey(newKey));
		}

		[Test()]
		public void ShouldNotGet()
		{
			Types.Transaction transaction = Util.GetNewTransaction(1);

			var mempool = new TxMempool();

			var keyedTransaction =
				new Keyed<Types.Transaction>(Merkle.transactionHasher.Invoke(transaction), transaction);

			Exception getException = null;

			try
			{
				mempool.Get(keyedTransaction.Key);
			}
			catch (Exception e)
			{
				getException = e;
			}

			Assert.NotNull(getException);
		}

		[Test()]
		public void ShouldGet()
		{
			Types.Transaction transaction = Util.GetNewTransaction(1);

			var mempool = new TxMempool();

			var keyedTransaction =
				new Keyed<Types.Transaction>(Merkle.transactionHasher.Invoke(transaction), transaction);

			mempool.Add(keyedTransaction);

			Types.Transaction getTransaction = mempool.Get(keyedTransaction.Key).Value;

			Assert.AreEqual(getTransaction, keyedTransaction.Value);
		}

		[Test()]
		public void CanCheckDoubleSpend()
		{
			var p = new TestTransactionPool();

			p.Add("base", 1);
			p.Add("tx1", 0);
			p.Spend("tx1", "base", 0);
			p.Add("tx2", 0);
			p.Spend("tx2", "base", 0);

			p.Render();

			var mempool = new TxMempool();

			mempool.Add(p["base"]);

			Assert.IsFalse(mempool.ContainsInputs(p["tx1"]));
			Assert.IsFalse(mempool.ContainsInputs(p["tx2"]));

			mempool.Add(p["tx1"]);

			Assert.IsTrue(mempool.ContainsInputs(p["tx1"]));
			Assert.IsTrue(mempool.ContainsInputs(p["tx2"]));
		}

		[Test()]
		public void CanGetOrphanedsOfTx()
		{
			var p = new TestTransactionPool();

			p.Add("parent", 1);
			p.Add("orphan", 0);
			p.Spend("orphan", "parent", 0);

			p.Render();

			var mempool = new TxMempool();

			Assert.IsFalse(mempool.GetOrphanedsOf(p["parent"]).Contains(p["orphan"]));

			mempool.Add(p["orphan"], true);

			Assert.IsTrue(mempool.GetOrphanedsOf(p["parent"]).Contains(p["orphan"]));
		}
	}
}