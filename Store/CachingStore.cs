//using System.Collections.Generic;

//namespace Store
//{
//	public abstract class CachingStore<T> :  where T : class
//	{
//		private EnumerableStore<T> _Store;
//		private readonly IDictionary<byte[], T> _Values;
//		private readonly TransactionContext _TransactionContext;

//		protected CachingStore(TransactionContext transactionContext, string tableName) : base(tableName)
//		{
//			_Store = new Store
//			_Values = new Dictionary<byte[], T>();
//			_TransactionContext = transactionContext;

//			foreach (Keyed<T> t in All(_TransactionContext))
//			{
//				_Values.Add(t.Key, t.Value);
//			}
//		}


//	}
//}