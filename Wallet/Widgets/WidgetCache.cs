using System;
using System.Collections.Generic;
using Gtk;

namespace Wallet
{
	public class WidgetCache
	{
		public delegate T Factory<T>() where T : Widget;

		private IDictionary<Type, WeakReference<Widget>> dictionary = new Dictionary<Type, WeakReference<Widget>>();

		public T Get<T>(Factory<T> factory) where T : Widget {
			if (!Contains<T>()) {
				Put<T>(factory());
			}

			return Get<T>();
		}

		private bool Contains<T>() {
			return dictionary.ContainsKey (typeof(T));
		}

		private void Put<T>(Widget value) where T : Widget {
			dictionary [typeof(T)] = new WeakReference<Widget>(value);
		}

		private T Get<T>() where T : Widget {
			Widget widget;

			dictionary [typeof(T)].TryGetTarget (out widget);

			return (T) widget;
		}

	}
}

