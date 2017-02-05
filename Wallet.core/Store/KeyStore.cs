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
		public KeyStore() : base("key")
		{
		}

		public bool AddKey(TransactionContext context, string base64EncodedPrivateKey)
		{
			var key = Key.Create(base64EncodedPrivateKey);

			if (ContainsKey(context, key.Private))
			{
				return false;
			}

			Put(context, new Keyed<Key>(key.Private, key));

			return true;
		}

		public Key GetUnusedKey(TransactionContext context) {
			var list = List (context, false, false);

			if (list.Count == 0) {
				var key = Key.Create();
				Put(context, new Keyed<Key>(key.Private, key));

				return key;
			} else {
				return list [0];
			}
		}

		public List<Key> List(TransactionContext context, bool? used = null, bool? isChange = null)
		{
			return All (context)
				.Where (v => (!used.HasValue || v.Value.Used == used.Value) && (!isChange.HasValue || v.Value.Change == isChange.Value))
				.Select (t => t.Value)
				.ToList ();
		}

		//public bool Find(TransactionContext context, Types.Output output, bool markAsUsed) {
		//	foreach (var key in List(context)) {
		//		if (key.IsMatch(output.@lock))
		//		{
		//			if (!key.Used && markAsUsed)
		//			{
		//				key.Used = true;
		//				Put(context, new Keyed<Key>(key.Private, key));
		//			}
		//			return true;
		//		}
		//	}

		//	return false;
		//}

		public void Used(TransactionContext context, Key key, bool? isChange)
		{
			key.Used = true;

			if (isChange.HasValue)
			{
				key.Change = isChange.Value;
			}

			Put(context, new Keyed<Key>(key.Private, key));
		}
	}
}
