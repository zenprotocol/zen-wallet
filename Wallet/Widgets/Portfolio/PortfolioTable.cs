using System;
using System.Collections.Generic;
using System.Linq;
using Gtk;
using Wallet.Constants;
using Wallet.core;

namespace Wallet.Widgets.Portfolio
{
    [System.ComponentModel.ToolboxItem(true)]
    public partial class PortfolioTable : WidgetBase, IPortfolioVIew, IAssetsView
	{
		readonly AssetsController _AssetsController;
		readonly DeltasController _DeltasController;

		UpdatingStore<byte[]> listStore = new UpdatingStore<byte[]>(
			0,
			typeof(byte[]),
			typeof(string),
			typeof(long)
		);

		public PortfolioTable()
        {
            this.Build();

			_DeltasController = new DeltasController(this);
			_AssetsController = new AssetsController(this);

			foreach (Widget w in new Widget[] { eventbox4, eventbox7 })
			{
				w.ModifyBg(Gtk.StateType.Normal, Colors.Base.Gdk);
			}

			ConfigureList();
		}

		private void ConfigureList()
		{
            listPortfolio.Model = listStore;

			listPortfolio.Selection.Mode = SelectionMode.None;
			listPortfolio.BorderWidth = 0;
			listPortfolio.HeadersVisible = false;
			listPortfolio.ModifyBase(Gtk.StateType.Active, Constants.Colors.Base.Gdk);
			listPortfolio.ModifyBase(Gtk.StateType.Selected, Constants.Colors.Base.Gdk);
			listPortfolio.ModifyBase(Gtk.StateType.Normal, Constants.Colors.ButtonSelected.Gdk);

			var col = new Gtk.TreeViewColumn();
			var rowRenderer = new PortfolioEntryRenderer();
			col.PackStart(rowRenderer, true);
			col.SetCellDataFunc(rowRenderer, new Gtk.TreeCellDataFunc(RenderCell));
			listPortfolio.AppendColumn(col);

            var headersStore = new Gtk.ListStore(typeof(object));
            headersStore.AppendValues(new object());
			listHeaders.Selection.Mode = SelectionMode.None;
			listHeaders.Model = headersStore;
			listHeaders.RulesHint = true; //alternating colors
			listHeaders.BorderWidth = 0;
			listHeaders.HeadersVisible = false;
			listHeaders.ModifyBase(Gtk.StateType.Active, Constants.Colors.Base.Gdk);
			listHeaders.ModifyBase(Gtk.StateType.Selected, Constants.Colors.Base.Gdk);
			listHeaders.ModifyBase(Gtk.StateType.Normal, Constants.Colors.ButtonSelected.Gdk);

			var colHeaders = new Gtk.TreeViewColumn();
            var rowRendererHeaders = new PortfolioHeaderRenderer();
			colHeaders.PackStart(rowRendererHeaders, true);
			listHeaders.AppendColumn(colHeaders);
		}

        void RenderCell(Gtk.TreeViewColumn column, Gtk.CellRenderer cellRenderer, Gtk.TreeModel model, Gtk.TreeIter iter)
        {
            var rowRenderer = cellRenderer as PortfolioEntryRenderer;

            rowRenderer.Asset = (string)model.GetValue(iter, 1);
            rowRenderer.Value = (long)model.GetValue(iter, 2);
        }

		public ICollection<AssetMetadata> Assets
		{
			set
			{
                foreach (var item in value)
                    if (!item.Asset.SequenceEqual(Consensus.Tests.zhash))
                        AssetUpdated = item;
			}
		}

		public AssetMetadata AssetUpdated
		{
			set
			{
				if (!value.Asset.SequenceEqual(Consensus.Tests.zhash))
                    listStore.Upsert(t => t.SequenceEqual(value.Asset), value.Asset, value.Display);
			}
		}

		public AssetDeltas PortfolioDeltas
		{
			set
			{
				listStore.Clear();
				
				foreach (var item in value)
				{
					if (!item.Key.SequenceEqual(Consensus.Tests.zhash))
					{
						listStore.Upsert(t => t.SequenceEqual(item.Key), item.Key, App.Instance.AssetsMetadata.TryGetValue(item.Key), item.Value);
					}
				}
			}
		}
    }
}
