using System;
using Gtk;

namespace Wallet
{
	public interface IMainMenuView {
		string Default { set; }
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

			HeightRequest = 100;
		}

		public override MenuButton Selection { 
			set {
				FindParent<MainWindow>().MainWindowController.MainAreaSelected = value.Name;
			}
		}

		public string Default
		{
			set
			{
				foreach (Widget widget in hboxContainer)
				{
					if (((MenuButton)widget).ImageName == value)
						((MenuButton)widget).Select();
				}
			}
		}
	}
}

