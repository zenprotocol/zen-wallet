using System;
namespace Store
{
	public class KeylessValueStore<TValue> //where TValue : new() //TODO: could remove constraint and return null/default on non existing Row 
	{
		private readonly String _TableName;

		public KeylessValueStore(String tableName)
		{
			_TableName = tableName;
		}

		public KeylessValueStoreContext<TValue> Context(TransactionContext context)
		{
			return new KeylessValueStoreContext<TValue>(context, _TableName);
		}
	}

	public class KeylessValueStoreContext<TValue> //where TValue : new()
	{
		private const byte KEY = 0x00;
		private readonly String _TableName;
		private readonly TransactionContext _Context;

		public KeylessValueStoreContext(TransactionContext context, String tableName)
		{
			_Context = context;
			_TableName = tableName;
		}

		public TValue Value
		{
			get
			{
				var valueRow = _Context.Transaction.Select<byte, TValue>(_TableName, KEY);
				TValue returnValue = valueRow.Exists ? valueRow.Value : default(TValue);
				Trace.Read(_TableName, KEY, returnValue);
				return returnValue;
			}
			set
			{
				Trace.Write(_TableName, KEY, value);
				_Context.Transaction.Insert<byte, TValue>(_TableName, KEY, value);
			}
		}
	}

	public class ValueStore<TKey, TValue> where TValue : new() //TODO: could remove constraint and return null/default on non existing Row 
	{
		private readonly String _TableName;

		public ValueStore(String tableName)
		{
			_TableName = tableName;
		}

		public ValueStoreContext<TKey, TValue> Context(TransactionContext context)
		{
			return new ValueStoreContext<TKey, TValue>(context, _TableName);
		}
	}

	public class ValueStoreContext<TKey, TValue> where TValue : new()
	{
		private readonly String _TableName;
		private readonly TransactionContext _Context;

		public ValueStoreContext(TransactionContext context, String tableName)
		{
			_Context = context;
			_TableName = tableName;
		}

		public TValue this[TKey key]
		{
			get
			{
				var valueRow = _Context.Transaction.Select<TKey, TValue>(_TableName, key);
				TValue returnValue = valueRow.Exists ? valueRow.Value : new TValue();
				Trace.Read(_TableName, key, returnValue);
				return returnValue;
			}
			set
			{
				Trace.Write(_TableName, key, value);
				_Context.Transaction.Insert<TKey, TValue>(_TableName, key, value);
			}
		}
	}
}
