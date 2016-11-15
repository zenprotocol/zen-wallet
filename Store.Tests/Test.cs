using NUnit.Framework;
using System;
using Consensus;
using System.Collections.Generic;
using Microsoft.FSharp.Collections;
using System.Linq;

namespace Store.Tests
{
	[TestFixture()]
	public class Test
	{
		[Test()]
		public void CanGetAndPutTransaction()
		{
			TransactionStore transactionStore = new TransactionStore("test", "test");

			Types.Transaction putTransaction = GetNewTransaction(1);

			transactionStore.Put(putTransaction);

			var key = Merkle.transactionHasher.Invoke(putTransaction);

			Types.Transaction getTransaction = transactionStore.Get(key);

			Assert.AreEqual(getTransaction.version, putTransaction.version);

			var reKey = Merkle.transactionHasher.Invoke(getTransaction);

			Assert.AreEqual(key, reKey);
		}


		private static Types.Transaction GetNewTransaction(uint version)
		{
			var endpoints = new List<Types.Outpoint>();
			var outputs = new List<Types.Output>();
			var hashes = new List<byte[]>();

			endpoints.Add(new Types.Outpoint(new byte[] { 0x34 }, 111));

			Types.Transaction transaction =
				new Types.Transaction(version,
					ListModule.OfSeq(endpoints),
					ListModule.OfSeq(hashes),
					ListModule.OfSeq(outputs),
					null);

			return transaction;
		}

	}
}
