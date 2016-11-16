using NUnit.Framework;
using System;
using Consensus;
using System.Linq;

namespace Store.Tests
{
	[TestFixture()]
	public class TxMempoolTests : TestBase
	{
		[Test()]
		public void ShouldContainsKey()
		{
			Types.Transaction transaction = GetNewTransaction(1);

			TxMempool mempool = new TxMempool();

			Keyed<Types.Transaction> keyedTransaction =
				new Keyed<Types.Transaction>(transaction, Merkle.transactionHasher.Invoke(transaction));

			mempool.Add(keyedTransaction);

			byte[] newKey = Merkle.transactionHasher.Invoke(transaction);

			Assert.IsTrue(Enumerable.SequenceEqual(newKey, keyedTransaction.Key));
			Assert.IsTrue(mempool.ContainsKey(keyedTransaction.Key));
			Assert.IsTrue(mempool.ContainsKey(newKey));

		}

		//[Test()]
		//public void ShouldContainsInputs()
		//{
		//	Types.Transaction transaction = GetNewTransaction(1);

		//	TxMempool mempool = new TxMempool();

		//	Keyed<Types.Transaction> keyedTransaction =
		//		new Keyed<Types.Transaction>(transaction, Merkle.transactionHasher.Invoke(transaction));

		//	mempool.Add(keyedTransaction);

		//	Assert.IsTrue(mempool.ContainsKey(keyedTransaction.Key));
		//}

		[Test()]
		public void ShouldNotGet()
		{
			Types.Transaction transaction = GetNewTransaction(1);

			TxMempool mempool = new TxMempool();

			Keyed<Types.Transaction> keyedTransaction =
				new Keyed<Types.Transaction>(transaction, Merkle.transactionHasher.Invoke(transaction));

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
			Types.Transaction transaction = GetNewTransaction(1);

			TxMempool mempool = new TxMempool();

			Keyed<Types.Transaction> keyedTransaction =
				new Keyed<Types.Transaction>(transaction, Merkle.transactionHasher.Invoke(transaction));

			mempool.Add(keyedTransaction);

			Types.Transaction getTransaction = mempool.Get(keyedTransaction.Key);

			Assert.AreEqual(getTransaction, keyedTransaction.Value);
		}
	}
}
