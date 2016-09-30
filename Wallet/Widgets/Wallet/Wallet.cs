using System;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class Wallet : WidgetBase, IFocusable
	{
		public Wallet ()
		{
			this.Build ();
		}

		public void Focus() {
			FindChild<Transactions>().Focus();
		}
	}
}

