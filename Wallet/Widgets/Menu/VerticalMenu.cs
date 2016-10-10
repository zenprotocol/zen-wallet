using System;
using Gtk;
using System.Collections.Generic;

namespace Wallet
{
	public interface IVerticalMenu : IMenu, IAssetsView {
		bool AllVisible { set; }
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class VerticalMenu : MenuBase, IVerticalMenu
	{
		public VerticalMenu ()
		{
			this.Build ();

			AssetsManager.GetInstance().InitAssetsView(this);
			MainAreaController.GetInstance().VerticalMenuView = this;

			WidthRequest = 170;
		}
			
		public override String Selection { 
			set {
				WalletController.GetInstance().CurrencySelected = value;
			}
		}

		public List<AssetType> Assets { 
			set {
				AddButton("All");

				foreach (AssetType assetType in value) {
					AddButton(assetType.Caption);
				}
			} 
		}

		public bool AllVisible { 
			set {
				FindChild<Gtk.VBox> ().Children [0].Visible = value;
			} 
		}

		private void AddButton(String caption) {
			MenuButton menuButton = new MenuButton ();

			menuButton.Name = caption;
			menuButton.Caption = caption;
			menuButton.Selected = false;

			FindChild<Gtk.VBox>().PackStart(menuButton, true, true, 0);
		}
	}
}