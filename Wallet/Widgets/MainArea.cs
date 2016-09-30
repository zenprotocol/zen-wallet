using System;

namespace Wallet
{
	public interface MainAreaView {
		int Page { set; }
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class MainArea : Gtk.Bin, MainAreaView
	{
		MainAreaController MainAreaController = MainAreaController.GetInstance ();

		public MainArea ()
		{
			this.Build ();
			MainAreaController.MainAreaView = this;
		}

		public int Page { 
			set {
				notebook1.Page = value;

				IFocusable focusable = notebook1.Children [value] as IFocusable;

				if (focusable != null) {
					focusable.Focus ();
				}
			}
		}
	}
}

