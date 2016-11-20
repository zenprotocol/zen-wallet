using System;
using System.Collections.Generic;
using Gtk;
using NodeCore;

namespace NodeTester
{
	public class ResourceOwnerWindow : Gtk.Window, IResourceOwner
	{
		private List<IDisposable> disposables = new List<IDisposable>();

		public void OwnResource(IDisposable disposable) {
			disposables.Add (disposable);
		}

		public ResourceOwnerWindow (WindowType WindowType) : base(WindowType)
		{
		}
			
		public void DisposeResources() {
			foreach (IDisposable disposable in disposables) {
				disposable.Dispose ();
			}
		}
	}
}

