using Gtk;
using Wallet.core;
using System.Linq;
using System;

namespace Wallet
{
	public interface IVerticalMenu {
		byte[] Asset { set; }
		bool AllVisible { set; }
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class VerticalMenu : MenuBase, IVerticalMenu
	{
		AssetsMetadata AssetsMetadata = App.Instance.Wallet.AssetsMetadata;

		public VerticalMenu ()
		{
			Build ();

			AssetsMetadata.AssetChanged += a =>
			{
				Application.Invoke(delegate
				{
					AddButton(a, AssetsMetadata[a]);
				});
			};

			foreach (var item in AssetsMetadata) {
				AddButton(item.Key, item.Value);
			}

			MainAreaController.Instance.VerticalMenuView = this;

			WidthRequest = 170;
		}
			
		public override MenuButton Selection { 
			set {
				WalletController.Instance.Asset = (byte[])value.Data;
				BalancesController.Instance.Asset = (byte[])value.Data;
			}
		}

		public bool AllVisible { 
			set {
				vboxContainer.Children [0].Visible = value;
			} 
		}

		public byte[] Asset
		{
			set
			{
				foreach (var child in vboxContainer.Children)
				{
					var buttonChild = (MenuButton)child;
					if (((byte[])buttonChild.Data).SequenceEqual(value))
						buttonChild.Select();
				}
				WalletController.Instance.Asset = value;
				BalancesController.Instance.Asset = value;
			}
		}

		void AddButton(byte[] hash, AssetType assetType) {
			foreach (var child in vboxContainer.Children)
			{
				var buttonChild = (MenuButton)child;
				if (((byte[])buttonChild.Data).SequenceEqual(hash))
					vboxContainer.Remove(buttonChild);
			}

			var menuButton = new MenuButton()
			{
				Data = hash,
				ImageFileName = assetType.Image,
				Caption = assetType.Caption
			};

			vboxContainer.PackStart(menuButton, true, true, 0);
			menuButton.Show();
		}
	}
}