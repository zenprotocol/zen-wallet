using System;
using System.IO;
using Store;

namespace BlockChain.Tests
{
	public class TestBase
	{
		protected class TestDBContext : IDisposable
		{
			public string DbName { get; private set; }
			private readonly DBContext _DBContext;

			public TestDBContext()
			{
				DbName = "test-" + new Random().Next(0, 1000);
				_DBContext = new DBContext(DbName);
			}

			public void Dispose()
			{
				_DBContext.Dispose();
				Directory.Delete(DbName, true);
			}

			public TransactionContext GetTransactionContext()
			{
				return _DBContext.GetTransactionContext();
			}
		}	
	}
}
