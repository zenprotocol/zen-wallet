﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gtk;
using Wallet.core;

namespace Wallet
{
	public interface IPortfolioVIew : IDeltasVIew
	{
        AssetDeltas PortfolioDeltas { set; }
	}

    [System.ComponentModel.ToolboxItem (true)]
    public partial class Portfolio : WidgetBase, IPortfolioVIew, IAssetsView
    {
		readonly DeltasController _DeltasController;
        readonly AssetsController _AssetsController;

		UpdatingStore<byte[]> listStore = new UpdatingStore<byte[]>(
            0,
            typeof(byte[]),
            typeof(string),
            typeof(long)
        );

        public Portfolio ()
        {
            this.Build ();

			_DeltasController = new DeltasController(this);
            _AssetsController = new AssetsController(this);

			Apply((Label label) =>
            {
                label.ModifyFg(Gtk.StateType.Normal, Constants.Colors.SubText.Gdk);
                label.ModifyFont(Constants.Fonts.ActionBarSmall);
            }, label3, label2);

            Apply((Label label) =>
            {
                label.ModifyFg(Gtk.StateType.Normal, Constants.Colors.Text2.Gdk);
                label.ModifyFont(Constants.Fonts.ActionBarBig);
            }, labelZen);

            ConfigureList();
        }

        private void ConfigureList()
        {
            treeview1.Model = listStore;

            treeview1.RulesHint = true; //alternating colors
            treeview1.Selection.Mode = SelectionMode.Single;
            //treeview1.Selection.Changed += OnSelectionChanged;
            treeview1.BorderWidth = 0;
            treeview1.HeadersVisible = false;
            treeview1.ModifyBase(Gtk.StateType.Active, Constants.Colors.Base.Gdk);
            treeview1.ModifyBase(Gtk.StateType.Selected, Constants.Colors.Base.Gdk);
            treeview1.ModifyBase(Gtk.StateType.Normal, Constants.Colors.Base.Gdk);

            var col = new Gtk.TreeViewColumn();
            var rowRenderer = new RowRenderer();
            col.PackStart(rowRenderer, true);
            col.SetCellDataFunc(rowRenderer, new Gtk.TreeCellDataFunc(RenderCell));
            col.MinWidth = 130;
            treeview1.AppendColumn(col);
        }

        void RenderCell(Gtk.TreeViewColumn column, Gtk.CellRenderer cellRenderer, Gtk.TreeModel model, Gtk.TreeIter iter)
        {
            var rowRenderer = cellRenderer as RowRenderer;

            rowRenderer.Asset = (string)model.GetValue(iter, 1);
            rowRenderer.Value = (long)model.GetValue(iter, 2);
        }

        public AssetDeltas PortfolioDeltas 
        { 
            set 
            {
				listStore.Clear();
				labelZen.Text = "";

				foreach (var item in value)
				{
					if (item.Key.SequenceEqual(Consensus.Tests.zhash))
					{
						labelZen.Text = $"{new Zen(item.Value)} {AssetsMetadata.ZEN}";
					}
					else
					{
                        listStore.Update(t => t.SequenceEqual(item.Key), item.Key, App.Instance.AssetsMetadata.TryGetValue(item.Key), item.Value);
					}
				}
			} 
        }

        public ICollection<AssetMetadata> Assets
        {
            set
            {
                foreach (var item in value)
                    if (!item.Asset.SequenceEqual(Consensus.Tests.zhash))
						listStore.Update(t => t.SequenceEqual(item.Asset), item.Asset, item.Display);
			}
        }

        public AssetMetadata AssetUpdated 
        {
            set
            {
				if (!value.Asset.SequenceEqual(Consensus.Tests.zhash))
					listStore.UpdateColumn(t => t.SequenceEqual(value.Asset), 1, value.Display);
            }
        }
    }
}

