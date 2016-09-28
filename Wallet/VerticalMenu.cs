using System;
using Gtk;

namespace Wallet
{
	public interface ITestTabsBarVertView : IMenu {
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class VerticalMenu : Gtk.Bin, ITestTabsBarVertView
	{
		WalletController WalletController = WalletController.GetInstance ();

		public VerticalMenu ()
		{
			this.Build ();

			WalletController.TestTabsBarVertView = this;
	
			WidthRequest = 170;
		}
			
		public String Selection { 
			set {
				WalletController.CurrencySelected = value;
			}
		}

		public int Default { 
			set {
				((MenuButton) ((Container)Children[0]).Children[value]).Select();
			}
		}
	}
}