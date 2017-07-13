using Consensus;
using Store;
using Wallet.core.Data;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Linq;

namespace Wallet.core.Store
{
	public class KeysManager
	{
		
	}

	public class KeyStore : MsgPackStore<Key>
	{
		public KeyStore() : base("wallet-key")
		{
		}

		public bool AddKey(TransactionContext context, string base64EncodedPrivateKey)
		{
			var key = Key.Create(base64EncodedPrivateKey);

			if (ContainsKey(context, key.Private))
			{
				return false;
			}

			Put(context, key.Private, key);

			return true;
		}

		/// <summary>
		/// Creates a new key if all are marked as used.
		/// </summary>
		/// <returns><c>true</c>, if unused key was created, <c>false</c> otherwise.</returns>
		/// <param name="context">Context.</param>
		/// <param name="isChange">Is change.</param>
		public bool GetUnusedKey(TransactionContext context, out Key key, bool? isChange = null) {
			var list = List (context, false, isChange);

			if (list.Count == 0) {
				var _key = Key.Create();
				Put(context, _key.Private, _key);

				key = _key;
				return true;
			} else {
				key = list [0];
				return false;
			}
		}

		public List<Key> List(TransactionContext context, bool? used = null, bool? isChange = null)
		{
			return All (context)
				.Where (v => (!used.HasValue || v.Item2.Used == used.Value) && (!isChange.HasValue || v.Item2.Change == isChange.Value))
				.Select (t => t.Item2)
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

			Put(context, key.Private, key);
		}
	}
}
