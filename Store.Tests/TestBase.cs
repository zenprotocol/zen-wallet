using System;
using System.Collections.Generic;
using System.IO;
using BlockChain.Database;
using Consensus;
using Microsoft.FSharp.Collections;

namespace BlockChain.Tests
{
	public class TestBase
	{
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
