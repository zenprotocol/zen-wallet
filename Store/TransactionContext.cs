using System;
using DBreeze;
using DBreeze.Transactions;

namespace Store
{
	public class TransactionContext : IDisposable
	{
		private DBContext _Engine;
		public Transaction Transaction { get; private set; }

		public TransactionContext(DBContext engine, Transaction transaction)
		{
			_Engine = engine;
			Transaction = transaction;
		}

		public void Commit()
		{
			Transaction.Commit();
		}

		public void Dispose()
		{
			_Engine.Remove(this);
			Transaction.Dispose();
		}
	}
}
