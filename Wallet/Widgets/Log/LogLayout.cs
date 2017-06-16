using System;
using System.Collections.Generic;
using System.Linq;
using Gtk;
using Wallet.core;

namespace Wallet
{
	public interface IAssetsView
	{
        ICollection<AssetMetadata> Assets { set; }
		AssetMetadata AssetUpdated { set; }
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

		public AssetMetadata AssetUpdated
		{
			set
            {
                Gtk.Application.Invoke(delegate
				{
				    try
				    {
				        TreeIter iter;
				        _ComboboxStore.GetIterFirst(out iter);
				        bool found = false;

				        do
				        {
				            var key = new GLib.Value();
				            _ComboboxStore.GetValue(iter, 0, ref key);
				            byte[] _asset = key.Val as byte[];

				            if (_asset != null && _asset.SequenceEqual(value.Asset))
				            {
				                _ComboboxStore.SetValue(iter, 1, value.Display);
				                found = true;
				                break;
				            }
				        } while (_ComboboxStore.IterNext(ref iter));

				        if (!found)
				        {
				            _ComboboxStore.AppendValues(value.Asset, value.Display);
				        }
				    }
				    catch
				    {
				        Console.WriteLine("Exception in portfolio AssetMatadataChanged handler");
				    }
				});
            }
		}

		public ICollection<AssetMetadata> Assets
		{
			set
			{
				var i = 0;

				foreach (var _asset in value)
				{
					if (_asset.Asset.SequenceEqual(WalletController.Instance.Asset))
					{
						_SelectedIdx = i;
					}
					else
					{
						i++;
					}

					_ComboboxStore.AppendValues(_asset.Asset, _asset.Display);
				}
			}
		}
	}
}
