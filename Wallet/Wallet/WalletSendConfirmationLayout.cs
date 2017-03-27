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

			spinbuttonAmount.Xalign = 1;
			spinbuttonAmount.ModifyFg(StateType.Normal, Constants.Colors.Text2.Gdk);
			spinbuttonAmount.ModifyFont(Constants.Fonts.ActionBarSmall);

			Apply((EventBox eventbox) =>
			{
				eventbox.ModifyBg(StateType.Normal, Constants.Colors.Textbox.Gdk);
			}, eventboxDestination, eventboxAsset, eventboxAmount);

			Apply((Label label) =>
			{
				label.ModifyFg(StateType.Normal, Constants.Colors.SubText.Gdk);
				label.ModifyFont(Constants.Fonts.ActionBarIntermediate);
			}, labelDestination, labelAsset, labelAmount);

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

			//TODO: refactor
			ExposeEvent += delegate
			{
				var assetType = App.Instance.Wallet.AssetsMetadata[WalletSendLayout.SendInfo.Asset];

				imageAsset.Pixbuf = ImagesCache.Instance.GetIcon(assetType.Image);
				labelSelectedAsset.Text = labelSelectedAsset1.Text = assetType.Caption;

				spinbuttonAmount.Value = WalletSendLayout.SendInfo.Amount;
				entryDestination.Text = BitConverter.ToString(WalletSendLayout.SendInfo.Destination);
			};

			buttonConfirm.Clicked += delegate {
				
			};
		}

		void Back(object sender, EventArgs e)
		{
			FindParent<Notebook>().Page -= 1;
		}
	}
}
