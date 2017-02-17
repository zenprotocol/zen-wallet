using System;
using System.Threading;
using DBreeze;

namespace Store
{
	public class DBContext : IDisposable
	{
		private System.Collections.Generic.List<TransactionContext> _List = new System.Collections.Generic.List<TransactionContext>();
		private DBreezeEngine _Engine;
		private ManualResetEvent _Event = new ManualResetEvent(false);

		public DBContext(string dbName)
		{
			_Engine = new DBreezeEngine(dbName);
		}

		public TransactionContext GetTransactionContext()
		{
			lock (_List)
			{
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
			_Engine.Dispose();
		}

		public void Wait()
		{
			_Event.WaitOne();
		}

	}
}
