using System;
using System.Collections.Generic;
using System.IO;
using Consensus;
using Microsoft.FSharp.Collections;

namespace Store.Tests
{
	public class TestBase
	{
		protected Types.Transaction GetNewTransaction(uint version)
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

		protected class TestStore : IDisposable
		{
			public TransactionStore Store { get; private set; }
			private readonly string _DbName;

			public TestStore()
			{
				Random random = new Random();
				_DbName = "test-" + random.Next(0, 1000).ToString();

				Store = new TransactionStore(_DbName, "test");
			}

			public void Dispose()
			{
				Directory.Delete(_DbName, true);
			}
		}	
	}
}
