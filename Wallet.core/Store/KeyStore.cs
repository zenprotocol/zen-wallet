using System;
using Consensus;
using Store;
using Wallet.core.Data;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Wallet.core.Store
{
	public class KeyStore : MsgPackStore<Key>
	{
		private DBContext _DBContext;

		public KeyStore(DBContext dbContext) : base("key")
		{
			_DBContext = dbContext;
			EnsureSingleKey ();
		}

		public IList<Key> List(bool? used = null, bool? isChange = null)
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
			
		private void EnsureSingleKey()
		{
			using (var transaction = _DBContext.GetTransactionContext())
			{
				if (All (transaction).Count() == 0) {
					Key key = Create ();
					Put(transaction, new Keyed<Key>(key.Public, key));
				}

				transaction.Commit();
			}
		}

		private Key Create() {
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

		public override string ToString ()
		{
			return this.GetType() + "\n" + JsonConvert.SerializeObject(
				List(), Formatting.Indented,
				new JsonConverter[] {new StringEnumConverter()});
		}
	}
}
