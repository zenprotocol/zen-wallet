using System;

namespace Wallet
{
//	enum MainAreaViewEnum {
//		Wallet,
//		Portfolio,
//		XXX,
//		YYY
//	}

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
			//	notebook1.
			}
		}
	}
}

