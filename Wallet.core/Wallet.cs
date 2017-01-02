using System;
using System.Collections.Generic;
using Infrastructure;
using Store;
using System.Linq;
using Wallet.core.Store;
using Wallet.core.Data;
using Consensus;

namespace Wallet.core
{
//	public class Wallet : Singleton<Wallet>, IDisposable
	public class WalletManager : IDisposable
	{
		private const string DB_NAME = "wallet";
		private KeyStore _KeyStore;
		private DBContext _DBContext;


		public delegate Action<Types.Transaction> OnNewTransaction();
//		public delegate Action<Types.Trasaction> OnNewBlock();
	

		public WalletManager()
		{
			_DBContext = new DBContext(DB_NAME);
			_KeyStore = new KeyStore();
		}

		public IEnumerable<Key> GetKeys(bool? used = null, bool? isChange = null)
		{
			return _KeyStore.All(_DBContext.GetTransactionContext())
				            .Where(v => (!used.HasValue || v.Value.Used == used.Value) && (!isChange.HasValue || v.Value.Change == isChange.Value))
				            .Select(t => t.Value);
		}

		public void AddKey(Key key)
		{
			using (var transaction = _DBContext.GetTransactionContext())
			{
				_KeyStore.Put(transaction, new Keyed<Key>(key.Public, key));
				transaction.Commit();
			}
		}

		public void EnsureKey()
		{
			using (var transaction = _DBContext.GetTransactionContext())
			{
				if (_KeyStore.All (transaction).Count() == 0) {
					Key key = CreateKey ();
					_KeyStore.Put(transaction, new Keyed<Key>(key.Public, key));
				}

				transaction.Commit();
			}
		}

		public Key CreateKey() {
			Random random = new Random ();

			byte[] privateBytes = new byte[32];
			random.NextBytes (privateBytes);

			byte[] publicBytes = Consensus.Merkle.hashHasher.Invoke (privateBytes);
			byte[] addressBytes = Consensus.Merkle.hashHasher.Invoke (publicBytes);

			return new Key () {
				Public = publicBytes,
				Private = privateBytes
			};
		}

		public void Dispose()
		{
			_DBContext.Dispose();
		}
	}
}
