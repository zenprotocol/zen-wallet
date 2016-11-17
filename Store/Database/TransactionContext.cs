using System;
using DBreeze.Transactions;

namespace BlockChain.Database
{
	public class TransactionContext : IDisposable
	{
		public Transaction Transaction { get; private set; }

		public TransactionContext(Transaction transaction)
		{
			Transaction = transaction;
		}

		public void Commit()
		{
			Transaction.Commit();
		}

		public void Dispose()
		{
			Transaction.Dispose();
		}
	}
}
