using System;
using DBreeze;

namespace Store
{
	public class DBContext : IDisposable
	{
		private DBreezeEngine _Engine;

		public DBContext(string dbName)
		{
			_Engine = new DBreezeEngine(dbName);
		}

		public TransactionContext GetTransactionContext()
		{
			return new TransactionContext(_Engine.GetTransaction());
		}

		public void Dispose()
		{
			_Engine.Dispose();
		}
	}
}
