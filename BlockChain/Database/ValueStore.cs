using System;

namespace BlockChain.Database
{
	//public class Field<TKey, TValue> where TValue : new()
	//{
	//	private readonly TransactionContext _Context;
	//	private readonly String _TableName;
	//	private readonly TKey _Key;

	//	public Field(TransactionContext context, String tableName, TKey key)
	//	{
	//		_Context = context;
	//		_TableName = tableName;
	//		_Key = key;
	//	}

	//	public TValue Value
	//	{
	//		get
	//		{
	//			var valueRow = _Context.Transaction.Select<TKey, TValue>(_TableName, _Key);
	//			return valueRow.Exists ? valueRow.Value : new TValue();
	//		}
	//		set
	//		{
	//			_Context.Transaction.Insert<TKey, TValue>(_TableName, _Key, value);
	//		}
	//	}
	//}

	public class ValueStore<TKey, TValue> where TValue : new() //TODO: could remove constraint and return null on non existing Row 
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
				DatabaseTrace.Read(_TableName, key, returnValue);
				return returnValue;
			}
			set
			{
				DatabaseTrace.Write(_TableName, key, value);
				_Context.Transaction.Insert<TKey, TValue>(_TableName, key, value);
			}
		}
	}
}