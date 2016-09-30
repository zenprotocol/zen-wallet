using System;
using Gtk;

namespace Wallet
{
	public interface TestTabsBarView : IMenu {
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class MainMenu : MenuBase, TestTabsBarView
	{
		MainAreaController MainAreaController = MainAreaController.GetInstance ();

		public MainMenu ()
		{
			this.Build ();
			MainAreaController.TestTabsBarView = this;

			HeightRequest = 100;
		}

		public override String Selection { 
			set {
				MainAreaController.MainAreaSelected = value;
			}
		}
	}
}

