using System;
using System.Collections.Generic;
using System.Linq;
using Gtk;

namespace Wallet
{
	public interface IAssetsView
	{
		IEnumerable<Tuple<byte[], String>> Assets { set; }
		Tuple<byte[], String> Asset { set; }
	}
	
	[System.ComponentModel.ToolboxItem(true)]
	public partial class LogLayout : Gtk.Bin, IAssetsView
	{
		int _SelectedIdx = 0;
		ListStore _ComboboxStore;

		public LogLayout()
		{
			this.Build();
			label1.ModifyFg(Gtk.StateType.Normal, Constants.Colors.Text.Gdk);

            _ComboboxStore = new ListStore(typeof(byte[]), typeof(string));

            comboboxAsset.Model = _ComboboxStore;
			var textRenderer = new CellRendererText();
            comboboxAsset.PackStart(textRenderer, false);
			comboboxAsset.AddAttribute(textRenderer, "text", 1);

			Gtk.TreeIter iter;
			comboboxAsset.Model.IterNthChild(out iter, _SelectedIdx);
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

			BalancesController.Instance.AssetsView = this;
		}

		public Tuple<byte[], string> Asset
		{
			set
			{
				TreeIter iter;
				_ComboboxStore.GetIterFirst(out iter);

				do
				{
					var key = new GLib.Value();
					_ComboboxStore.GetValue(iter, 0, ref key);
					byte[] _asset = key.Val as byte[];

					if (_asset.SequenceEqual(value.Item1))
					{
						_ComboboxStore.SetValue(iter, 1, value.Item2);
						break;
					}
				} while (_ComboboxStore.IterNext(ref iter));
			}
		}

		public IEnumerable<Tuple<byte[], string>> Assets
		{
			set
			{
				var i = 0;

				foreach (var _asset in value)
				{
					if (_asset.Item1.SequenceEqual(WalletController.Instance.Asset))
					{
						_SelectedIdx = i;
					}
					else
					{
						i++;
					}

					_ComboboxStore.AppendValues(_asset.Item1, _asset.Item2);
				}
			}
		}
	}
}
