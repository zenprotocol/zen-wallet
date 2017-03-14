using System;
using Gtk;

namespace Wallet
{
	public interface IMainMenuView {
		int Default { set; }
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class MainMenu : MenuBase, IMainMenuView
	{
		public MainMenu ()
		{
			this.Build ();

			foreach (Widget widget in hboxContainer) {
				((MenuButton)widget).ImageName = widget.Name;
			}

			MainAreaController.Instance.MainMenuView = this;

			HeightRequest = 100;
		}

		public override MenuButton Selection { 
			set {
				MainAreaController.Instance.MainAreaSelected = value.Name;
			}
		}
	}
}

