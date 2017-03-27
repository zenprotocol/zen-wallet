using System;
using System.Collections.Generic;
using Gdk;
using Gtk;
using QRCoder;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class WalletSendLayout : WidgetBase
	{
		static Dictionary<string, Gdk.Pixbuf> _IconsCache = new Dictionary<string, Pixbuf>();
		static Gdk.Pixbuf GetIcon(string image)
		{
			if (!_IconsCache.ContainsKey(image))
			{
				_IconsCache[image] = new Pixbuf(image).ScaleSimple(32, 32, InterpType.Hyper);
			}

			return _IconsCache[image];
		}

		public WalletSendLayout()
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
				label.ModifyFont(Constants.Fonts.ActionBarSmall);
			}, labelDestination, labelAsset, labelSelectedAsset, labelSelectedAsset1, labelSelectOtherAsset, labelAmount);

			Apply((Entry entry) =>
			{
				entry.ModifyFg(StateType.Normal, Constants.Colors.Text2.Gdk);
				entry.ModifyFont(Constants.Fonts.ActionBarSmall);
			}, entryAddress);	

			buttonClose.Clicked += Back;

			var i = 0;
			int selectedIdx = 0;
			var keys = new Dictionary<string, byte[]>();
			foreach (var _asset in App.Instance.Wallet.AssetsMetadata)
			{
				keys[_asset.Value.Caption] = _asset.Key;

				if (WalletController.Instance.AssetType.Caption == _asset.Value.Caption)
				{
					selectedIdx = i;
				}
				else
				{
					i++;
				}

				comboboxAsset.AppendText(_asset.Value.Caption);
			}

			TreeIter iter;
			comboboxAsset.Model.IterNthChild(out iter, selectedIdx);
			comboboxAsset.SetActiveIter(iter);

			comboboxAsset.Changed += (sender, e) =>
			{
				var comboBox = sender as Gtk.ComboBox;

				comboBox.GetActiveIter(out iter);
				var value = new GLib.Value();
				comboBox.Model.GetValue(iter, 0, ref value);

				var assetType = App.Instance.Wallet.AssetsMetadata[keys[value.Val as string]];

				labelSelectedAsset.Text = labelSelectedAsset1.Text = assetType.Caption;
				imageAsset.Pixbuf = GetIcon(assetType.Image);
			};
		}

		void Back(object sender, EventArgs e)
		{
			FindParent<Notebook>().Page = 0;
		}
	}
}
