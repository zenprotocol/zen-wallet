using System;
using Gtk;

namespace Wallet
{
	public abstract class WidgetBase : Gtk.Bin
	{
		private WidgetCache parentsCache = new WidgetCache();
		private WidgetCache childrenCache = new WidgetCache();

		public T FindParent<T>() where T : Widget
		{
			return parentsCache.Get<T>(() => {
				return FindParentRecursive<T>(this);
			});
		}

		public T FindChild<T>(int index = 0) where T : Widget
		{
			return childrenCache.Get<T>(() => {
				return FindChildRecursive<T>(this, index);
			});
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

