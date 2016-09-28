using System;
using Gtk;

namespace Wallet
{
	public interface TestTabsBarView : IMenu {
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class MainMenu : Gtk.Bin, TestTabsBarView
	{
		MainAreaController MainAreaController = MainAreaController.GetInstance ();

		public MainMenu ()
		{
			this.Build ();
			MainAreaController.TestTabsBarView = this;

			HeightRequest = 100;
		}

		public String Selection { 
			set {
				MainAreaController.MainAreaSelected = value;
			}
		}

		public int Default { 
			set {
				((MenuButton) ((Container)Children[0]).Children[value]).Select();
			}
		}
	}
}

