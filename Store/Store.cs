using System;
using System.Collections.Generic;

namespace Store
{
	//TODO: memory management
	//https://docs.google.com/document/d/1IFkXoX3Tc2zHNAQN9EmGSXZGbQabMrWmpmVxFsLxLsw/pub
	//calling GC.Collect
	//calling table.Close

	//public abstract class Store<TWrapper, TItem> where TWrapper : StoredItem<TItem>
	public abstract class Store<T> where T : class
	{
		protected readonly string _TableName;

		protected Store(string tableName)
		{
			_TableName = tableName;
		}

		public void Put(TransactionContext transactionContext, Keyed<T> item) //TODO: used Keyed?
		{
			Put(transactionContext, new Keyed<T>[] { item });
		}

		private void Put(TransactionContext transactionContext, byte[] key, byte[] value)
		{
			Put(transactionContext, new Tuple<byte[],byte[]>[] { new Tuple<byte[], byte[]>(key, value) });
		}

		private void Put(TransactionContext transactionContext, Keyed<T>[] items)
		{
			foreach (Keyed<T> item in items) {
				Trace.Write(_TableName, item.Key);
				transactionContext.Transaction.Insert<byte[], byte[]> (_TableName, item.Key, Pack(item.Value));
			}
		}

		public void Put(TransactionContext transactionContext, Tuple<byte[], byte[]>[] items)
		{
			foreach (Tuple<byte[], byte[]> item in items)
			{
				Trace.Write(_TableName, item.Item1);
				transactionContext.Transaction.Insert<byte[], byte[]>(_TableName, item.Item1, item.Item2);
			}
		}

		public bool ContainsKey(TransactionContext transactionContext, byte[] key)
		{
			Trace.KeyLookup(_TableName, key);
			return transactionContext.Transaction.Select<byte[], byte[]>(_TableName, key).Exists;
		}

		public Keyed<T> Get(TransactionContext transactionContext, byte[] key)
		{
			Trace.Read(_TableName, key);
			var row = transactionContext.Transaction.Select<byte[], byte[]>(_TableName, key);
			return row.Exists ? new Keyed<T>(key, Unpack(row.Value, row.Key)) : null;
		}

		public void Remove(TransactionContext transactionContext, byte[] key)
		{
			transactionContext.Transaction.RemoveKey<byte[]>(_TableName, key);
		}

		public IEnumerable<Keyed<T>> All(TransactionContext transactionContext)
		{
			foreach (var row in transactionContext.Transaction.SelectForward<byte[], byte[]>(_TableName))
			{
				yield return new Keyed<T>(row.Key, Unpack(row.Value, row.Key));
			}
		}

		public void RemoveAll(TransactionContext transactionContext)
		{
			foreach (var row in transactionContext.Transaction.SelectForward<byte[], byte[]>(_TableName))
			{
				transactionContext.Transaction.RemoveKey<byte[]>(_TableName, row.Key);
			}
		}

		protected abstract byte[] Pack(T item);
		protected abstract T Unpack(byte[] data, byte[] key);
	}
}