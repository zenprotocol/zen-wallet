using System;
using Gtk;
using System.Collections.Generic;

namespace Wallet
{
	public interface IVerticalMenu : IMenu {
		bool AllVisible { set; }
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class VerticalMenu : MenuBase, IVerticalMenu
	{
		public VerticalMenu ()
		{
			this.Build ();

			foreach (String key in AssetsManager.AssetTypes.Keys) {
				AddButton(key, AssetsManager.AssetTypes[key]);
			}

			MainAreaController.GetInstance().VerticalMenuView = this;

			WidthRequest = 170;
		}
			
		public override MenuButton Selection { 
			set {
				WalletController.GetInstance().Asset = AssetsManager.AssetTypes[value.Name];
			}
		}

		public bool AllVisible { 
			set {
				vboxContainer.Children [0].Visible = value;
			} 
		}

		private void AddButton(String key, AssetType assetType) {
			vboxContainer.PackStart(new MenuButton () { 
				Name = key, 
				Caption = assetType.Caption 
			}, true, true, 0);
		}
	}
}