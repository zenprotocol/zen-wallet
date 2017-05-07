using System;
using System.Collections.Generic;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class LogLayout : Gtk.Bin
	{
		public LogLayout()
		{
			this.Build();
			label1.ModifyFg(Gtk.StateType.Normal, Constants.Colors.Text.Gdk);

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

			Gtk.TreeIter iter;
			comboboxAsset.Model.IterNthChild(out iter, selectedIdx);
			comboboxAsset.SetActiveIter(iter);

			comboboxAsset.Changed += (sender, e) =>
			{
				var comboBox = sender as Gtk.ComboBox;

				comboBox.GetActiveIter(out iter);
				var value = new GLib.Value();
				comboBox.Model.GetValue(iter, 0, ref value);

				BalancesController.Instance.Asset = keys[value.Val as string];
			};
		}
	}
}
