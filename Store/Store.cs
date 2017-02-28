using System;
using System.Collections.Generic;

namespace Store
{
	public abstract class Store<TKey, TValue> //where T : class
	{
		protected readonly string _TableName;

		protected Store(string tableName)
		{
			_TableName = tableName;
		}

		public void Put(TransactionContext transactionContext, TKey key, TValue item) //TODO: use Keyed?
		{
			var _key = Pack(key);
			Trace.Write(_TableName, _key);
			transactionContext.Transaction.Insert<byte[], byte[]>(_TableName, _key, Pack(item));
		}

		public bool ContainsKey(TransactionContext transactionContext, TKey key)
		{
			var _key = Pack(key);
			Trace.KeyLookup(_TableName, _key);
			return transactionContext.Transaction.Select<byte[], byte[]>(_TableName, _key).Exists;
		}

		public Keyed<TKey, TValue> Get(TransactionContext transactionContext, TKey key)
		{
			var _key = Pack(key);
			Trace.Read(_TableName, _key);
			var row = transactionContext.Transaction.Select<byte[], byte[]>(_TableName, _key);
			return row.Exists ? new Keyed<TKey, TValue>(Unpack<TKey>(row.Key), Unpack<TValue>(row.Value)) : null;
		}

		public void Remove(TransactionContext transactionContext, TKey key)
		{
			transactionContext.Transaction.RemoveKey(_TableName, Pack(key));
		}

		public void Count(TransactionContext transactionContext)
		{
			transactionContext.Transaction.Count(_TableName);
		}
		//public IEnumerable<Keyed<T>> All(TransactionContext transactionContext)
		//{
		//	foreach (var row in transactionContext.Transaction.SelectForward<byte[], byte[]>(_TableName))
		//	{
		//		yield return new Keyed<T>(row.Key, Unpack(row.Value, row.Key));
		//	}
		//}

		public IEnumerable<Keyed<TKey, TValue>> All(TransactionContext transactionContext, Func<TValue, bool> predicate = null, bool syncronized = false)
		{
			if (syncronized)
			{
				//TODO: this WILL cause an exception when hit more than once per tx
				transactionContext.Transaction.SynchronizeTables(_TableName);
			}

			foreach (var row in transactionContext.Transaction.SelectForward<byte[], byte[]>(_TableName))
			{
				var key = Unpack<TKey>(row.Key);
				var value = Unpack<TValue>(row.Value);

				if (predicate == null || predicate(value))
				{
					yield return new Keyed<TKey, TValue>(key, value);
				}
			}
		}

		public void RemoveAll(TransactionContext transactionContext)
		{
			foreach (var row in transactionContext.Transaction.SelectForward<byte[], byte[]>(_TableName))
			{
				transactionContext.Transaction.RemoveKey<byte[]>(_TableName, row.Key);
			}
		}

		protected abstract byte[] Pack<T>(T value);
		protected abstract T Unpack<T>(byte[] data);
	}


	//TODO: memory management
	//https://docs.google.com/document/d/1IFkXoX3Tc2zHNAQN9EmGSXZGbQabMrWmpmVxFsLxLsw/pub
	//calling GC.Collect
	//calling table.Close

	//public abstract class Store<TWrapper, TItem> where TWrapper : StoredItem<TItem>
	public abstract class Store<T> : Store<byte[], T> //where T : class
	{
	//	protected readonly string _TableName;

		protected Store(string tableName) : base(tableName)
		{
		}
	}
}