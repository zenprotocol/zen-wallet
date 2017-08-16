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
    public partial class LogLayout : WidgetBase, IAssetsView, IStatementsVIew, IControlInit
	{
        public AssetsController AssetsController { get; set; }
        UpdatingStore<byte[]> _AssetsStore = new UpdatingStore<byte[]>(0, typeof(byte[]), typeof(string));

        static byte[] _CurrentAsset = Consensus.Tests.zhash;
        public byte[] CurrentAsset
        {
            get { return _CurrentAsset; }
            set { 
                _CurrentAsset = value;
                FindChild<Log>().SelectedAsset = _CurrentAsset;
                UpdateTotals();
            }
        }

		public LogLayout()
		{
			this.Build();

			new DeltasController(this);

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
                    CurrentAsset = (byte[])ctl.Model.GetValue(iter, 0);
			};
		}

		public AssetMetadata AssetUpdated
        {
            set
            {
                _AssetsStore.Upsert(t => t.SequenceEqual(value.Asset), value.Asset, value.Display);
            }
        }

		public ICollection<AssetMetadata> Assets
		{
			set
			{
                foreach (var _asset in value)
                {
                    _AssetsStore.AppendValues(_asset.Asset, _asset.Display);
                }

				TreeIter iterDefault;
				if (_AssetsStore.Find(t => t.SequenceEqual(Consensus.Tests.zhash), out iterDefault))
					comboboxAsset.SetActiveIter(iterDefault);
			}
		}

        List<TxDelta> _StatementsDeltas;
        public List<TxDelta> StatementsDeltas { 
            get {
                return _StatementsDeltas;
            }
            set
            {
                _StatementsDeltas = value;
                UpdateTotals();
            }
        }

        public void Init()
        {
			TreeIter storeIter;
			var canIter = comboboxAsset.Model.GetIterFirst(out storeIter);

			while (canIter)
			{
				var asset = (byte[])comboboxAsset.Model.GetValue(storeIter, 0);

                if (asset == Consensus.Tests.zhash)
				{
					comboboxAsset.SetActiveIter(storeIter);
                    CurrentAsset = asset;
					break;
				}

				canIter = comboboxAsset.Model.IterNext(ref storeIter);
			}
		}

        void UpdateTotals()
        {
			ulong sent = 0;
			ulong received = 0;
			long total = 0;

            if (StatementsDeltas != null)
                StatementsDeltas.ForEach(
    				txDelta => txDelta.AssetDeltas.Where(
                        assetDelta => assetDelta.Key.SequenceEqual(CurrentAsset)).ToList().ForEach(
    						assetDelta =>
    						{
    							total += assetDelta.Value;

    							var absValue = (ulong)Math.Abs(assetDelta.Value);

    							if (assetDelta.Value < 0)
    							{
    								sent += absValue;
    							}
    							else
    							{
    								received += absValue;
    							}
    						}));

			logtotals1.Totals = new Tuple<decimal, decimal, decimal>(received, sent, total);
        }
    }
}