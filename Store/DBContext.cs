using System;
using System.Configuration;
using System.Threading;
using DBreeze;

namespace Store
{
	public class DBContext : IDisposable
	{
		System.Collections.Generic.List<TransactionContext> _List = new System.Collections.Generic.List<TransactionContext>();
		DBreezeEngine _Engine = null;
		ManualResetEvent _Event = new ManualResetEvent(false);
		string _dbName;

		public DBContext(string dbName)
		{
			_dbName = dbName;
		}

		public TransactionContext GetTransactionContext()
		{
			lock (_List)
			{
				if (_Engine == null)
				{
					_Engine = new DBreezeEngine(_dbName);
				}

				var t = new TransactionContext(this, _Engine.GetTransaction());
				_List.Add(t);
				_Event.Reset();

				return t;
			}
		}

		public void Remove(TransactionContext transaction)
		{
			lock (_List)
			{
				_List.Remove(transaction);

				if (_List.Count == 0)
				{
					_Event.Set();
				}
			}
		}

		public void Dispose()
		{
			_Event.WaitOne();

			if (_Engine != null)
			{
				_Engine.Dispose();
				_Engine = null;
			}
		}

		public void Wait()
		{
			_Event.WaitOne();
		}

	}
}
