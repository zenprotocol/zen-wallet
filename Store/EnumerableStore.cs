using System;
using System;
using System.Collections.Generic;

namespace Store
{
	public abstract class EnumerableStore<T> : Store<T> where T : class
	{
		protected EnumerableStore(string tableName) : base(tableName)
		{
		}

		public IEnumerable<Keyed<T>> All(TransactionContext transactionContext)
		{
			foreach (var row in transactionContext.Transaction.SelectForward<byte[], byte[]>(_TableName))
			{
				yield return new Keyed<T>(row.Key, Unpack(row.Value, row.Key));
			}
		}
	}
}
