using System;
using System.Collections.Generic;
using Gtk;

namespace Wallet
{
	public class WidgetBase : Gtk.Bin
	{
		private IDictionary<Type, Widget> parentsCache = new Dictionary<Type, Widget>();
		private IDictionary<Type, Widget> childrenCache = new Dictionary<Type, Widget>();


		public T FindParent<T>() where T : Widget
		{
			Type type = typeof(T);

			if (!parentsCache.ContainsKey(type)) {
				parentsCache[type] = FindParentRecursive<T>(this);
			}

			return (T) parentsCache[type];
		}

		public T FindChild<T>(int index = 0) where T : Widget
		{
			Type type = typeof(T);

			if (!childrenCache.ContainsKey(type)) {
				childrenCache[type] = FindChildRecursive<T>(this, index);
			}
				
			return (T) childrenCache[type];
		}

		private T FindChildRecursive<T>(Container container, int index) where T : Widget {
			int i = 0;

			foreach (Widget child in container) {
				if (child is T) {
					if (i == index) {
						return (T)child;
					} else {
						i++;
					}
				}
			}

			foreach (Widget child in container) {
				if (child is Container) {
					return FindChildRecursive<T>(child as Container, index);
				}
			}

			throw new Exception();
		}

		private T FindParentRecursive<T>(Widget widget) where T : Widget {
			Widget parentWidget = widget.Parent;

			if (parentWidget is T) {
				return (T) parentWidget;
			}

			return FindParentRecursive<T>(parentWidget);

		}
	}
}

