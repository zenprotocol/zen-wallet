using System;
using BlockChain.Data;
using DBreeze;
using DBreeze.Transactions;

namespace BlockChain.Database
{
	//TODO: memory management
	//https://docs.google.com/document/d/1IFkXoX3Tc2zHNAQN9EmGSXZGbQabMrWmpmVxFsLxLsw/pub
	//calling GC.Collect
	//calling table.Close

	//public abstract class Store<TWrapper, TItem> where TWrapper : StoredItem<TItem>
	public abstract class Store<T> where T : class
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
				DatabaseTrace.Write(_TableName, storedItem.Key);
				transactionContext.Transaction.Insert<byte[], byte[]> (_TableName, storedItem.Key, storedItem.Data);
			}
		}

		public void Put(TransactionContext transactionContext, Tuple<byte[], byte[]>[] items)
		{
			foreach (Tuple<byte[], byte[]> item in items)
			{
				DatabaseTrace.Write(_TableName, item.Item1);
				transactionContext.Transaction.Insert<byte[], byte[]>(_TableName, item.Item1, item.Item2);
			}
		}

		public bool ContainsKey(TransactionContext transactionContext, byte[] key)
		{
			DatabaseTrace.KeyLookup(_TableName, key);
			return transactionContext.Transaction.Select<byte[], byte[]>(_TableName, key).Exists;
		}

		public T Get(TransactionContext transactionContext, byte[] key)
		{
			DatabaseTrace.Read(_TableName, key);
			var row = transactionContext.Transaction.Select<byte[], byte[]>(_TableName, key);
			return row.Exists ? FromBytes(row.Value, row.Key) : null;
		}

		protected abstract StoredItem<T> Wrap(T item);
		protected abstract T FromBytes(byte[] data, byte[] key);
	}
}