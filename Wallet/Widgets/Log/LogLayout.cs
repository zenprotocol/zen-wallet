using System;
using System.Collections.Generic;
using System.Linq;
using Gtk;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class LogLayout : Gtk.Bin
	{
		public LogLayout()
		{
			this.Build();
			label1.ModifyFg(Gtk.StateType.Normal, Constants.Colors.Text.Gdk);

            var comboboxStore = new ListStore(typeof(byte[]), typeof(string));

			var i = 0;
			int selectedIdx = 0;

            comboboxAsset.Model = comboboxStore;
			var textRenderer = new CellRendererText();
            comboboxAsset.PackStart(textRenderer, false);
			comboboxAsset.AddAttribute(textRenderer, "text", 1);

			foreach (var _asset in App.Instance.Wallet.AssetsMetadata.Keys)
			{
				if (_asset.SequenceEqual(WalletController.Instance.Asset))
				{
					selectedIdx = i;
				}
				else
				{
					i++;
				}

                var _iter = comboboxStore.AppendValues(_asset, Convert.ToBase64String(_asset));
				App.Instance.Wallet.AssetsMetadata.Get(_asset).ContinueWith(t =>
				{
					Gtk.Application.Invoke(delegate
					{
						comboboxStore.SetValue(_iter, 1, t.Result);
					});
				});
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
                byte[] _asset = value.Val as byte[];
                BalancesController.Instance.Asset = _asset;
			};
		}
	}
}
