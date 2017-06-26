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
            var found = Find(keyMatchPredicate, out iter);

			if (found)
            {
                SetValues(iter, values);
            }
            else
			{
				AppendValues(values);
			}
		}

        public bool Find(Predicate<TKey> keyMatchPredicate, out TreeIter iter)
        {
			var canIter = GetIterFirst(out iter);

			while (canIter)
			{
				var keyValue = new GLib.Value();
				GetValue(iter, _KeyColumn, ref keyValue);
				TKey oValue = keyValue.Val as TKey;

				if (keyMatchPredicate(oValue))
				{
                    return true;
				}

				canIter = IterNext(ref iter);
			}

            return false;
        }
	}
}
