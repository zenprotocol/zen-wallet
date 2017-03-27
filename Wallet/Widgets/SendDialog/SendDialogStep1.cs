using System;
using System.Collections.Generic;
using Consensus;
using Wallet.core.Data;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class SendDialogStep1 : WidgetBase
	{
		byte[] asset;

		public SendDialogStep1()
		{
			this.Build();

			dialogfieldAmount.Caption = "AMOUNT";
			dialogfieldTo.Caption = "TO";
			dialogcombofieldAsset.Caption = "ASSET";

			var i = 0;
			int selectedIdx = 0;
			var keys = new Dictionary<string, byte[]>();
			foreach (var _asset in App.Instance.Wallet.AssetsMetadata)
			{
				keys[_asset.Value.Caption] = _asset.Key;

				if (WalletController.Instance.AssetType.Caption == _asset.Value.Caption)
				{
					selectedIdx = i;
					asset = _asset.Key;
				}
				else
				{
					i++;
				}

				dialogcombofieldAsset.ComboBox.AppendText(_asset.Value.Caption);
			}

			Gtk.TreeIter iter;
			dialogcombofieldAsset.ComboBox.Model.IterNthChild(out iter, selectedIdx);
			dialogcombofieldAsset.ComboBox.SetActiveIter(iter);

			dialogcombofieldAsset.ComboBox.Changed += (sender, e) =>
			{
				var comboBox = sender as Gtk.ComboBox;

				comboBox.GetActiveIter(out iter);
				var value = new GLib.Value();
				comboBox.Model.GetValue(iter, 0, ref value);
				asset = keys[value.Val as string];
			};

			eventboxSend.ButtonReleaseEvent += (object o, Gtk.ButtonReleaseEventArgs args) =>
			{
				ulong amount;
				byte[] address;

				try
				{
					amount = ulong.Parse(dialogfieldAmount.Value);
				}
				catch
				{
					labelMessage.Text = "Invalid amount";
					return;
				}

				if (amount <= 0)
				{
					labelMessage.Text = "Invalid amount";
					return;
				}

				try
				{
					address = Key.FromBase64String(dialogfieldTo.Value);
				}
				catch
				{
					labelMessage.Text = "Invalid address";
					return;
				}

				Types.Transaction tx;

				if (App.Instance.Wallet.Sign(
					address,
					asset,
					amount,
					out tx
				))
				{
					FindParent<SendDialog>().Next(tx);
				}
				else
				{
					labelMessage.Text = "Not enough " + App.Instance.Wallet.AssetsMetadata[asset];
				}
			};

			buttonRaw.Clicked += delegate
			{
				new SendRaw().ShowDialog();
			};
		}
	}
}

