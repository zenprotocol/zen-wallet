using System;
using System.Collections.Generic;
using System.Linq;
using DBreeze;

namespace Store
{
	public class StoredItem<T>
	{
		public T Value { get; private set; }
		public byte[] Key { get; private set; }
		public byte[] Data { get; private set; }

		public StoredItem(T value, byte[] key, byte[] data) {
			Value = value;
			Key = key;
			Data = data;
		}
	}

	public abstract class Store<T> : IDisposable
	{
		private readonly DBreezeEngine _Engine;
		private readonly string _TableName;

		protected Store(string dbName, string tableName)
		{
			if (tableName == null) throw new ArgumentNullException("tableName");
			if (dbName == null) throw new ArgumentNullException("dbName");

			_TableName = tableName;
			_Engine = new DBreezeEngine(dbName);
		}

		public void Put(T item)
		{
			Put(new T[] { item });
		}

		public void Put(T[] items)
		{
			using (var transaction = _Engine.GetTransaction())
			{
				foreach (T item in items) {
					StoredItem<T> storedItem = Wrap(item);
					transaction.Insert<byte[], byte[]> (_TableName, storedItem.Key, storedItem.Data);
				}
				transaction.Commit();
			}
		}

		public T Get(byte[] key)
		{
			using (var transaction = _Engine.GetTransaction())
			{
				foreach (var row in transaction.SelectForward<byte[], byte[]>(_TableName)) {
					return FromBytes(row.Value, row.Key);
				}
			}

			throw new Exception("not found");
		}

		protected abstract StoredItem<T> Wrap(T item);
		protected abstract T FromBytes(byte[] data, byte[] key);

		public void Dispose()
		{
			_Engine.Dispose();
		}

		//public IEnumerator<T> GetEnumerator()
		//{
		//	using (var transaction = _Engine.GetTransaction())
		//	{
		//		foreach (var row in transaction.SelectForward<byte[], byte[]>(TableName)) {
		//			yield return FromBytes(row.Value, row.Key);
		//		}
		//	}
		//}

		//System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		//{
		//	return this.GetEnumerator();
		//}
	}
}