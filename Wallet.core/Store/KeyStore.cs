using System;
using Consensus;
using Store;
using Wallet.core.Data;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Sodium;

namespace Wallet.core.Store
{
	public class KeyStore : MsgPackStore<Key>
	{
		private DBContext _DBContext;

		public KeyStore(DBContext dbContext) : base("key")
		{
			_DBContext = dbContext;
		}

		public Key GetKey(bool isChange = false) {
			var list = List (false, isChange);

			if (list.Count == 0) {
				Key key = null;

				using (var transaction = _DBContext.GetTransactionContext())
				{
					if (All (transaction).Count() == 0) {
						key = Create (isChange);
						Put(transaction, new Keyed<Key>(key.Public, key));
					}

					transaction.Commit();
				}

				return key;
			} else {
				return list [0];
			}
		}

		public List<Key> List(bool? used = null, bool? isChange = null)
		{
			return All (_DBContext.GetTransactionContext ())
				.Where (v => (!used.HasValue || v.Value.Used == used.Value) && (!isChange.HasValue || v.Value.Change == isChange.Value))
				.Select (t => t.Value)
				.ToList ();
		}

		public bool IsMatch(Types.Output output) {
			foreach (var key in List()) {
				if (output.@lock is Types.OutputLock.PKLock) {
					Types.OutputLock.PKLock pkLock = output.@lock as Types.OutputLock.PKLock;

					if (key.Public.SequenceEqual(pkLock.pkHash)) {
						return true;
					}
				}
			}

			return false;
		}
			
		private Key Create(bool isChange) {
			var keyPair = PublicKeyAuth.GenerateKeyPair();
			var addressBytes = Consensus.Merkle.hashHasher.Invoke(keyPair.PublicKey);

			return new Key () {
				Public = addressBytes,
				Private = keyPair.PrivateKey,
				Used = false,
				Change = isChange
			};
		}

		public override string ToString ()
		{
			return this.GetType() + "\n" + JsonConvert.SerializeObject(
				List(), Formatting.Indented,
				new JsonConverter[] {new StringEnumConverter()});
		}
	}
}
