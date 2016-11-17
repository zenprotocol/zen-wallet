using System;
using BlockChain.Database;
using DBreeze;

namespace BlockChain.Database
{
	public class DBContext : IDisposable
	{
		public DBreezeEngine Engine { get; private set; }

		public DBContext(string dbName)
		{
			Engine = new DBreezeEngine(dbName);
		}

		public TransactionContext GetTransactionContext()
		{
			return new TransactionContext(Engine.GetTransaction());
		}

		public void Dispose()
		{
			Engine.Dispose();
		}
	}
}
