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
	public partial class LogLayout : WidgetBase, IAssetsView
	{
        public AssetsController AssetsController { get; set; }
        UpdatingStore<byte[]> _AssetsStore = new UpdatingStore<byte[]>(0, typeof(byte[]), typeof(string));

        static byte[] _CurrentAsset = Consensus.Tests.zhash;
        public static byte[] CurrentAsset
        {
            get { return _CurrentAsset; }
            set { _CurrentAsset = value; }
        }

		public LogLayout()
		{
			this.Build();

            CurrentAsset = Consensus.Tests.zhash;
            AssetsController = new AssetsController(this);

			labelHeader.ModifyFg(StateType.Normal, Constants.Colors.TextHeader.Gdk);
			labelHeader.ModifyFont(Constants.Fonts.ActionBarBig);

			comboboxAsset.Model = _AssetsStore;
			var textRenderer = new CellRendererText();
            comboboxAsset.PackStart(textRenderer, false);
			comboboxAsset.AddAttribute(textRenderer, "text", 1);

			comboboxAsset.Changed += (sender, e) =>
			{
				var ctl = sender as ComboBox;
				TreeIter iter;
				if (ctl.GetActiveIter(out iter))
					FindChild<Log>().SelectedAsset = (byte[])ctl.Model.GetValue(iter, 0);
			};
		}

		public AssetMetadata AssetUpdated
        {
            set
            {
                Gtk.Application.Invoke(delegate
                {
                    _AssetsStore.Update(t => t.SequenceEqual(value.Asset), value.Asset, value.Display);
                });
            }
        }

		public ICollection<AssetMetadata> Assets
		{
			set
			{
                Gtk.Application.Invoke(delegate
                {
                    foreach (var _asset in value)
                    {
                        _AssetsStore.AppendValues(_asset.Asset, _asset.Display);
                    }

					TreeIter iterDefault;
					if (_AssetsStore.Find(t => t.SequenceEqual(Consensus.Tests.zhash), out iterDefault))
						comboboxAsset.SetActiveIter(iterDefault);
				});
			}
		}

        public Tuple<decimal, decimal, decimal> Totals 
        {
            set 
            {
                logtotals1.Totals = value;
            }
        }
	}
}
