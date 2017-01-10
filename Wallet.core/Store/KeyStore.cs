using Consensus;
using Store;
using Wallet.core.Data;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Linq;

namespace Wallet.core.Store
{
	public class KeyStore : MsgPackStore<Key>
	{
		private DBContext _DBContext;

		public KeyStore(DBContext dbContext) : base("key")
		{
			_DBContext = dbContext;
		}

		public bool AddKey(string base64EncodedPrivateKey)
		{
			using (var transaction = _DBContext.GetTransactionContext())
			{
				var key = Key.Create(base64EncodedPrivateKey);

				if (ContainsKey(transaction, key.Private))
				{
					return false;
				}

				Put(transaction, new Keyed<Key>(key.Private, key));
				transaction.Commit();

				return true;
			}
		}

		public Key GetKey(bool isChange = false) {
			var list = List (false, isChange);

			if (list.Count == 0) {
				Key key = null;

				using (var transaction = _DBContext.GetTransactionContext())
				{
					if (All (transaction).Count() == 0) {
						key = new Key();
						Put(transaction, new Keyed<Key>(key.Private, key));
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
				if (key.IsMatch(output.@lock))
				{
					return true;
				}
			}

			return false;
		}
			
		public override string ToString ()
		{
			return this.GetType() + "\n" + JsonConvert.SerializeObject(
				List(), Formatting.Indented,
				new JsonConverter[] {new StringEnumConverter()});
		}
	}
}
