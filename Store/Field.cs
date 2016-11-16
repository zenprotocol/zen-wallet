using System;

namespace Store
{
	public class Field<TKey, TValue> where TValue : new()
	{
		private readonly TransactionContext _Context;
		private readonly String _TableName;
		private readonly TKey _Key;

		public Field(TransactionContext context, String tableName, TKey key)
		{
			_Context = context;
			_TableName = tableName;
			_Key = key;
		}

		public TValue Value
		{
			get
			{
				var valueRow = _Context.Transaction.Select<TKey, TValue>(_TableName, _Key);
				return valueRow.Exists ? valueRow.Value : new TValue();
			}
			set
			{
				_Context.Transaction.Insert<TKey, TValue>(_TableName, _Key, value);
			}
		}
	}
}