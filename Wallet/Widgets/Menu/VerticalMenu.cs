using System;
using Gtk;

namespace Wallet
{
	public interface IVerticalMenu : IMenu {
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class VerticalMenu : MenuBase, IVerticalMenu
	{
		MainAreaController MainAreaController = MainAreaController.GetInstance();

		public VerticalMenu ()
		{
			this.Build ();

			MainAreaController.VerticalMenuView = this;
			WidthRequest = 170;
		}
			
		public override String Selection { 
			set {
				WalletController.GetInstance ().CurrencySelected = value;
			}
		}
	}
}