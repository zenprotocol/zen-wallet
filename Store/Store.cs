using System;
using DBreeze;
using DBreeze.Transactions;

namespace Store
{
	public abstract class Store<T>
	{
		private readonly string _TableName;

		public Store(string tableName)
		{
			_TableName = tableName;
		}

		public void Put(TransactionContext transactionContext, T item)
		{
			Put(transactionContext, new T[] { item });
		}

		public void Put(TransactionContext transactionContext, byte[] key, byte[] value)
		{
			Put(transactionContext, new Tuple<byte[],byte[]>[] { new Tuple<byte[], byte[]>(key, value) });
		}

		public void Put(TransactionContext transactionContext, T[] items)
		{
			foreach (T item in items) {
				StoredItem<T> storedItem = Wrap(item);
				transactionContext.Transaction.Insert<byte[], byte[]> (_TableName, storedItem.Key, storedItem.Data);
			}
		}

		public void Put(TransactionContext transactionContext, Tuple<byte[], byte[]>[] items)
		{
			foreach (Tuple<byte[], byte[]> item in items)
			{
				transactionContext.Transaction.Insert<byte[], byte[]>(_TableName, item.Item1, item.Item2);
			}
		}

		public T Get(TransactionContext transactionContext, byte[] key)
		{
			var row = transactionContext.Transaction.Select<byte[], byte[]>(_TableName, key);
			return FromBytes(row.Value, row.Key);
		}

		protected abstract StoredItem<T> Wrap(T item);
		protected abstract T FromBytes(byte[] data, byte[] key);
	}
}