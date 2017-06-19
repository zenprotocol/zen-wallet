using System;
using Gtk;

namespace Wallet
{
	public class UpdatingStore<TKey> : ListStore where TKey : class
	{
		int _KeyColumn;

		public UpdatingStore(int keyColumn, params Type[] types) : base(types)
		{
			_KeyColumn = keyColumn;
		}

		public void Update(Predicate<TKey> keyMatchPredicate, params object[] values)
		{
			TreeIter iter;
			var canIter = GetIterFirst(out iter);
			var found = false;

			while (canIter)
			{
				var keyValue = new GLib.Value();
				GetValue(iter, _KeyColumn, ref keyValue);
				TKey oValue = keyValue.Val as TKey;

				if (keyMatchPredicate(oValue))
				{
					SetValues(iter, values);
					found = true;
					break;
				}

				canIter = IterNext(ref iter);
			}

			if (!found)
			{
				AppendValues(values);
			}
		}
	}
}
