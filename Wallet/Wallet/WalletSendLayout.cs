using System;
using System.Collections.Generic;
using Gdk;
using Gtk;
using QRCoder;
using Wallet.core;

namespace Wallet
{
	public class SendInfo
	{
		public byte[] Asset
		{
			get; set;
		}

		public double Amount
		{
			get; set;
		}
	
		public string Destination
		{
			get; set;
		}
	}

	[System.ComponentModel.ToolboxItem(true)]
	public partial class WalletSendLayout : WidgetBase, IPortfolioVIew
	{
		public static SendInfo SendInfo
		{
			get; private set;
		}

		AssetDeltas _AssetDeltas = null;

		public WalletSendLayout()
		{
			this.Build();
			SendInfo = new SendInfo();

			buttonPaste.Clicked += delegate {
				Clipboard clipboard = Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));

				var target = Gdk.Atom.Intern("text/plain", true);
				var selection = clipboard.WaitForContents(target);

				if (selection != null)
				{
					entryDestination.Text = System.Text.Encoding.UTF8.GetString(selection.Data, 0, selection.Data.Length);
				}
			};

			entryDestination.Changed += (sender, e) =>
			{
				SendInfo.Destination = ((Entry)sender).Text;
			};

			spinbuttonAmount.Xalign = 1;
			spinbuttonAmount.ModifyFg(StateType.Normal, Constants.Colors.Text2.Gdk);
			spinbuttonAmount.ModifyFont(Constants.Fonts.ActionBarSmall);
			spinbuttonAmount.ValueChanged += (sender, e) =>
			{
				SendInfo.Amount = ((SpinButton)sender).Value;
			};

			Apply((EventBox eventbox) =>
			{
				eventbox.ModifyBg(StateType.Normal, Constants.Colors.Textbox.Gdk);
			}, eventboxDestination, eventboxAsset, eventboxAmount);

			Apply((Label label) =>
			{
				label.ModifyFg(StateType.Normal, Constants.Colors.SubText.Gdk);
				label.ModifyFont(Constants.Fonts.ActionBarIntermediate);
			}, labelDestination, labelAsset, labelAmount, labelBalanceValue);

			Apply((Label label) =>
			{
				label.ModifyFg(StateType.Normal, Constants.Colors.SubText.Gdk);
				label.ModifyFont(Constants.Fonts.ActionBarSmall);
			}, labelSelectedAsset, labelSelectedAsset1, labelSelectOtherAsset, labelBalance);

			Apply((Entry entry) =>
			{
				entry.ModifyFg(StateType.Normal, Constants.Colors.Text2.Gdk);
				entry.ModifyFont(Constants.Fonts.ActionBarSmall);
			}, entryDestination);	

			buttonBack.Clicked += Back;

			var i = 0;
			int selectedIdx = 0;
			var keys = new Dictionary<string, byte[]>();
			foreach (var _asset in App.Instance.Wallet.AssetsMetadata)
			{
				keys[_asset.Value.Caption] = _asset.Key;

				if (WalletController.Instance.AssetType.Caption == _asset.Value.Caption)
				{
					SendInfo.Asset = _asset.Key;
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

				SendInfo.Asset = keys[value.Val as string]; 
				var assetType = App.Instance.Wallet.AssetsMetadata[SendInfo.Asset];

				labelSelectedAsset.Text = labelSelectedAsset1.Text = assetType.Caption;
				imageAsset.Pixbuf = ImagesCache.Instance.GetIcon(assetType.Image);

				UpdateBalance();
			};

			UpdateBalance();

			buttonSignAndReview.Clicked += delegate {
				FindParent<Notebook>().Page = 3;
			};

			PortfolioController.Instance.AddVIew(this);
		}

		void Back(object sender, EventArgs e)
		{
			FindParent<Notebook>().Page = 0;
		}

		public void Clear()
		{
			throw new NotImplementedException();
		}

		public void SetDeltas(AssetDeltas assetDeltas)
		{
			_AssetDeltas = assetDeltas;
			UpdateBalance();
		}

		void UpdateBalance()
		{
			var balance = _AssetDeltas == null || !_AssetDeltas.ContainsKey(SendInfo.Asset) ? 0 : _AssetDeltas[SendInfo.Asset];		
			labelBalanceValue.Text = $"{balance} {App.Instance.Wallet.AssetsMetadata[SendInfo.Asset]}";
		}
	}
}
