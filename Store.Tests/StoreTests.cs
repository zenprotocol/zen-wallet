using NUnit.Framework;
using System;
using Consensus;
using System.Collections.Generic;
using System.Linq;

namespace Store.Tests
{
	[TestFixture()]
	public class Test : TestBase
	{
		private Random random = new Random();

		[Test()]
		public void CanStoreSingleTransaction()
		{
			using (TestStore testStore = new TestStore())
			{
				for (int i = 0; i < 10; i++)
				{
					uint version = (uint)random.Next(0, 1000);

					var putTransaction = GetNewTransaction(version);
					var key = Merkle.transactionHasher.Invoke(putTransaction);

					testStore.Store.Put(putTransaction);
					var getTransaction = testStore.Store.Get(key);

					Assert.AreEqual(getTransaction.version, putTransaction.version);
					Assert.AreEqual(key, Merkle.transactionHasher.Invoke(getTransaction));
				}
			}
		}

		[Test()]
		public void CanStoreMultipleTransactions()
		{
			using (TestStore testStore = new TestStore())
			{
				List<Tuple<byte[], Types.Transaction>> putTransactions = new List<Tuple<byte[], Types.Transaction>>();

				for (int i = 0; i < 10; i++)
				{
					uint version = (uint)random.Next(0, 1000);

					var putTransaction = GetNewTransaction(version);
					var key = Merkle.transactionHasher.Invoke(putTransaction);

					putTransactions.Add(new Tuple<byte[], Types.Transaction>(key, putTransaction));
				}

                testStore.Store.Put(putTransactions.Select(t => t.Item2).ToArray());

				putTransactions.ForEach(t =>
				{
					var getTransaction = testStore.Store.Get(t.Item1);

					Assert.AreEqual(getTransaction.version, t.Item2.version);
					Assert.AreEqual(t.Item1, Merkle.transactionHasher.Invoke(getTransaction));
				});
			}
		}

		[Test()]
		public void CanStoreRawSingleRawTransaction()
		{
			using (TestStore testStore = new TestStore())
			{
				for (int i = 0; i < 10; i++)
				{
					uint version = (uint)random.Next(0, 1000);

					var putTransaction = GetNewTransaction(version);
					var key = Merkle.transactionHasher.Invoke(putTransaction);
					var value = Merkle.serialize<Types.Transaction>(putTransaction);

					testStore.Store.Put(key, value);
					var getTransaction = testStore.Store.Get(key);

					Assert.AreEqual(getTransaction.version, putTransaction.version);
					Assert.AreEqual(key, Merkle.transactionHasher.Invoke(getTransaction));
				}
			}
		}

		[Test()]
		public void CanStoreMultipleRawTransactions()
		{
			using (TestStore testStore = new TestStore())
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

				testStore.Store.Put(putRawTransactions.ToArray());

				putTransactions.ForEach(t =>
				{
					var getTransaction = testStore.Store.Get(t.Item1);

					Assert.AreEqual(getTransaction.version, t.Item2.version);
					Assert.AreEqual(t.Item1, Merkle.transactionHasher.Invoke(getTransaction));
				});
			}
		}
	}
}
