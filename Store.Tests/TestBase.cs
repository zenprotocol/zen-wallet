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

		protected class TestDBContext<T> : IDisposable where T : new()
		{
			public T Store { get; private set; }
			private readonly string _DbName;
			private readonly DBContext _DBContext;

			public TestDBContext()
			{
				_DbName = "test-" + new Random().Next(0, 1000);
				_DBContext = new DBContext(_DbName);
				Store = new T();
			}

			public void Dispose()
			{
				_DBContext.Dispose();
				Directory.Delete(_DbName, true);
			}

			public TransactionContext GetTransactionContext()
			{
				return _DBContext.GetTransactionContext();
			}
		}	
	}
}
