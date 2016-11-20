using System;
using System.Collections.Generic;
using Infrastructure;
using Store;
using Wallet.Store;
using System.Linq;

namespace Wallet
{
	public class Wallet : Singleton<Wallet>, IDisposable
	{
		private const string DB_NAME = "wallet";
		private KeyStore _KeyStore;
		private DBContext _DBContext;

		public Wallet()
		{
			_DBContext = new DBContext(DB_NAME);
		}

		public IEnumerable<Key> GetKeys()
		{
			return _KeyStore.All(_DBContext.GetTransactionContext()).Select(t => t.Value);
		}

		public void AddKey(Key key)
		{
			using (var transaction = _DBContext.GetTransactionContext())
			{
				_KeyStore.Put(transaction, key);
			}
		}

		public void Dispose()
		{
			_DBContext.Dispose();
		}
	}
}
