using System;
using Gtk;

namespace Wallet
{
	public interface ITestTabsBarVertView : IMenu {
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class VerticalMenu : MenuBase, ITestTabsBarVertView
	{
		WalletController WalletController = WalletController.GetInstance ();

		public VerticalMenu ()
		{
			this.Build ();

			WalletController.TestTabsBarVertView = this;
	
			WidthRequest = 170;
		}
			
		public override String Selection { 
			set {
				WalletController.CurrencySelected = value;
			}
		}
	}
}