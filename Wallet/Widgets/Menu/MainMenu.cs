using System;
using Gtk;

namespace Wallet
{
	public interface IMainMenuView : IMenu {
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class MainMenu : MenuBase, IMainMenuView
	{
		MainAreaController MainAreaController = MainAreaController.GetInstance();

		public MainMenu ()
		{
			this.Build ();

			foreach (Widget widget in hboxContainer) {
				((MenuButton)widget).ImageName = widget.Name;
			}

			MainAreaController.MainMenuView = this;

			HeightRequest = 100;
		}

		public override MenuButton Selection { 
			set {
				MainAreaController.MainAreaSelected = value.Name;
			}
		}
	}
}

