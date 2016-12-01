using System;
using System.Collections.Generic;
using Infrastructure;
using Store;
using System.Linq;
using Wallet.core.Store;
using Wallet.core.Data;

namespace Wallet.core
{
	public class Wallet : Singleton<Wallet>, IDisposable
	{
		private const string DB_NAME = "wallet";
		private KeyStore _KeyStore;
		private DBContext _DBContext;

		public Wallet()
		{
			_DBContext = new DBContext(DB_NAME);
			_KeyStore = new KeyStore();
		}

		public IEnumerable<Key> GetKeys(bool? used = null, bool? isChange = null)
		{
			return _KeyStore.All(_DBContext.GetTransactionContext())
				            .Where(v => (!used.HasValue || v.Value.Used == used.Value) && (!isChange.HasValue || v.Value.IsChange == isChange.Value))
				            .Select(t => t.Value);
		}

		public void AddKey(Key key)
		{
			//using (var transaction = _DBContext.GetTransactionContext())
			//{
			//	_KeyStore.Put(transaction, key);
			//	transaction.Commit();
			//}
		}

		public void Dispose()
		{
			_DBContext.Dispose();
		}
	}
}
