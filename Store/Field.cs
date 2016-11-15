using System;

namespace Store
{
	public class Field<T> where T : new()
	{
		private readonly TransactionContext _Context;
		private readonly String _TableName;
		private readonly String _Key;

		public Field(TransactionContext context, String tableName, String key)
		{
			_Context = context;
			_TableName = tableName;
			_Key = key;
		}

		public T Value
		{
			get
			{
				var valueRow = _Context.Transaction.Select<String, T>(_TableName, _Key);
				return valueRow.Exists ? valueRow.Value : new T();
			}
			set
			{
				_Context.Transaction.Insert<String, T>(_TableName, _Key, value);
			}
		}
	}
}