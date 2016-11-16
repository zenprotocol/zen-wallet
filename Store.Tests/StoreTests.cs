using NUnit.Framework;
using System;
using Consensus;
using System.Collections.Generic;
using System.Linq;

namespace Store.Tests
{
	[TestFixture()]
	public class StoreTests : TestBase
	{
		private Random random = new Random();

		[Test()]
		public void CanStoreSingleTransaction()
		{
			using (TestDBContext<TxStore> dbContext = new TestDBContext<TxStore>())
			{
				for (int i = 0; i < 10; i++)
				{
					uint version = (uint)random.Next(0, 1000);

					var putTransaction = GetNewTransaction(version);
					var key = Merkle.transactionHasher.Invoke(putTransaction);

					using (TransactionContext transaction = dbContext.GetTransactionContext())
					{
						dbContext.Store.Put(transaction, putTransaction);
						transaction.Commit();
					}

					using (TransactionContext transaction = dbContext.GetTransactionContext())
					{
						var getTransaction = dbContext.Store.Get(transaction, key);

						Assert.AreEqual(getTransaction.version, putTransaction.version);
						Assert.AreEqual(key, Merkle.transactionHasher.Invoke(getTransaction));
					}
				}
			}
		}

		[Test()]
		public void CanStoreMultipleTransactions()
		{
			using (TestDBContext<TxStore> dbContext = new TestDBContext<TxStore>())
			{
				List<Tuple<byte[], Types.Transaction>> putTransactions = new List<Tuple<byte[], Types.Transaction>>();

				for (int i = 0; i < 10; i++)
				{
					uint version = (uint)random.Next(0, 1000);

					var putTransaction = GetNewTransaction(version);
					var key = Merkle.transactionHasher.Invoke(putTransaction);

					putTransactions.Add(new Tuple<byte[], Types.Transaction>(key, putTransaction));
				}

				using (TransactionContext transaction = dbContext.GetTransactionContext())
				{
					dbContext.Store.Put(transaction, putTransactions.Select(t => t.Item2).ToArray());
					transaction.Commit();
				}

				using (TransactionContext transaction = dbContext.GetTransactionContext())
				{
					putTransactions.ForEach(t =>
					{
						var getTransaction = dbContext.Store.Get(transaction, t.Item1);

						Assert.AreEqual(getTransaction.version, t.Item2.version);
						Assert.AreEqual(t.Item1, Merkle.transactionHasher.Invoke(getTransaction));
					});
				}
			}
		}

		[Test()]
		public void CanStoreRawSingleRawTransaction()
		{
			using (TestDBContext<TxStore> dbContext = new TestDBContext<TxStore>())
			{
				for (int i = 0; i < 10; i++)
				{
					uint version = (uint)random.Next(0, 1000);

					var putTransaction = GetNewTransaction(version);
					var key = Merkle.transactionHasher.Invoke(putTransaction);
					var value = Merkle.serialize<Types.Transaction>(putTransaction);

					Types.Transaction getTransaction;

					using (TransactionContext transaction = dbContext.GetTransactionContext())
					{
						dbContext.Store.Put(transaction, key, value);
						getTransaction = dbContext.Store.Get(transaction, key);
						transaction.Commit();
					}

					using (TransactionContext transaction = dbContext.GetTransactionContext())
					{
						Assert.AreEqual(getTransaction.version, putTransaction.version);
						Assert.AreEqual(key, Merkle.transactionHasher.Invoke(getTransaction));
					}
				}
			}
		}

		[Test()]
		public void CanStoreMultipleRawTransactions()
		{
			using (TestDBContext<TxStore> dbContext = new TestDBContext<TxStore>())
			{
				List<Tuple<byte[], Types.Transaction>> putTransactions = new List<Tuple<byte[], Types.Transaction>>();
				List<Tuple<byte[], byte[]>> putRawTransactions = new List<Tuple<byte[], byte[]>>();

				for (int i = 0; i < 10; i++)
				{
					uint version = (uint)random.Next(0, 1000);

					var putTransaction = GetNewTransaction(version);
					var key = Merkle.transactionHasher.Invoke(putTransaction);
					var value = Merkle.serialize<Types.Transaction>(putTransaction);

					putTransactions.Add(new Tuple<byte[], Types.Transaction>(key, putTransaction));
					putRawTransactions.Add(new Tuple<byte[], byte[]>(key, value));
				}

				using (TransactionContext transaction = dbContext.GetTransactionContext())
				{
					dbContext.Store.Put(transaction, putRawTransactions.ToArray());
					transaction.Commit();
				}

				using (TransactionContext transaction = dbContext.GetTransactionContext())
				{
					putTransactions.ForEach(t =>
					{
						var getTransaction = dbContext.Store.Get(transaction, t.Item1);

						Assert.AreEqual(getTransaction.version, t.Item2.version);
						Assert.AreEqual(t.Item1, Merkle.transactionHasher.Invoke(getTransaction));
					});
				}
			}
		}
	}
}
