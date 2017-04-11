using System;
using System.Collections.Generic;
using Gdk;
using Gtk;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class WalletSendConfirmationLayout : WidgetBase
	{
		public WalletSendConfirmationLayout()
		{
			this.Build();

			Apply((Label label) =>
			{
				label.ModifyFg(StateType.Normal, Constants.Colors.Success.Gdk);
				label.ModifyFont(Constants.Fonts.ActionBarSmall);
			}, labelStatus);

			Apply((EventBox eventbox) =>
			{
				eventbox.ModifyBg(StateType.Normal, Constants.Colors.Textbox.Gdk);
			}, eventboxStatus, eventboxDestination, eventboxAsset, eventboxAmount);

			Apply((Label label) =>
			{
				label.ModifyFg(StateType.Normal, Constants.Colors.SubText.Gdk);
				label.ModifyFont(Constants.Fonts.ActionBarIntermediate);
			}, labelDestination, labelAsset, labelAmount, labelAmountValue);

			Apply((Label label) =>
			{
				label.ModifyFg(StateType.Normal, Constants.Colors.SubText.Gdk);
				label.ModifyFont(Constants.Fonts.ActionBarSmall);
			}, labelSelectedAsset, labelSelectedAsset1);

			Apply((Entry entry) =>
			{
				entry.ModifyFg(StateType.Normal, Constants.Colors.Text2.Gdk);
				entry.ModifyFont(Constants.Fonts.ActionBarSmall);
			}, entryDestination);

			buttonBack.Clicked += Back;

			buttonTransmit.Clicked += delegate {
				WalletSendLayout.SendInfo.Result = App.Instance.Node.Transmit(WalletSendLayout.Tx);
				UpdateStatus();
			};
		}

		public void Init()
		{
			var assetType = App.Instance.Wallet.AssetsMetadata[WalletSendLayout.SendInfo.Asset];

			imageAsset.Pixbuf = ImagesCache.Instance.GetIcon(assetType.Image);
			labelSelectedAsset.Text = labelSelectedAsset1.Text = assetType.Caption;

			labelAmountValue.Text = WalletSendLayout.SendInfo.Amount.ToString();
			entryDestination.Text = WalletSendLayout.SendInfo.Destination.ToString();

			UpdateStatus();
		}

		void Back(object sender, EventArgs e)
		{
			FindParent<Notebook>().Page -= 1;
		}

		void UpdateStatus()
		{
			if (WalletSendLayout.SendInfo.Result == null)
			{
				if (WalletSendLayout.SendInfo.Signed)
				{
					labelStatus.Text = "Transaction signed successfully.";
					labelStatus.ModifyFg(StateType.Normal, Constants.Colors.Success.Gdk);
				}
				else
				{
					labelStatus.Text = "Transaction signing error.";
					labelStatus.ModifyFg(StateType.Normal, Constants.Colors.Error.Gdk);
				}
			}
			else
			{
				if (WalletSendLayout.SendInfo.Result == BlockChain.BlockChain.TxResultEnum.Accepted)
				{
					labelStatus.ModifyFg(StateType.Normal, Constants.Colors.Success.Gdk);
					labelStatus.Text = "Transaction broadcasted successfully.";
				}
				else
				{
					labelStatus.ModifyFg(StateType.Normal, Constants.Colors.Error.Gdk);
					labelStatus.Text = "Transaction bbvoardcast failed, reason: " + WalletSendLayout.SendInfo.Result;
				}
			}
		}
	}
}
