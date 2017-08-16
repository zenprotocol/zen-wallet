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

		public void Upsert(Predicate<TKey> keyMatchPredicate, params object[] values)
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

        public void UpdateColumn(Predicate<TKey> keyMatchPredicate, int column, object value)
		{
			TreeIter iter;
			var found = Find(keyMatchPredicate, out iter);

			if (!found)
			{
                iter = Append();
			}

			SetValue(iter, column, value);
		}

		public bool Find(Predicate<TKey> keyMatchPredicate, out TreeIter iter)
        {
			var canIter = GetIterFirst(out iter);

			while (canIter)
			{
				var keyValue = new GLib.Value();
				GetValue(iter, _KeyColumn, ref keyValue);
				var oValue = keyValue.Val as TKey;

                if (oValue != null)
				{
                    try {
                        if (keyMatchPredicate(oValue))
                            return true;
                    } catch (Exception e)
                    {
                        Console.WriteLine("Find", e);
                    }
				}

				canIter = IterNext(ref iter);
			}

            return false;
        }
	}
}
