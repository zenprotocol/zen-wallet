using System;
using System.Collections.Generic;
using Gtk;
using Infrastructure;

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
			
		protected void DisposeResources() {
			foreach (IDisposable disposable in disposables) {
				disposable.Dispose ();
			}
		}
	}
}

