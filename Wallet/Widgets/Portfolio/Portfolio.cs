using System;
using System.Linq;
using System.Threading.Tasks;
using Gtk;
using Wallet.core;

namespace Wallet
{
    [System.ComponentModel.ToolboxItem (true)]
    public partial class Portfolio : WidgetBase, IPortfolioVIew
    {
        UpdatingStore<byte[]> listStore = new UpdatingStore<byte[]>(
            0,
            typeof(byte[]),
            typeof(string),
            typeof(decimal)
        );

        public Portfolio ()
        {
            this.Build ();

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

            PortfolioController.Instance.AddVIew(this);
    
            ConfigureList();

            App.Instance.Wallet.AssetsMetadata.AssetMatadataChanged += t =>
            {
                Gtk.Application.Invoke(delegate
                {
                    try
                    {
                        TreeIter iter;
                        listStore.GetIterFirst(out iter);

                        do
                        {
                            var key = new GLib.Value();
                            listStore.GetValue(iter, 0, ref key);
                            byte[] _asset = key.Val as byte[];

                            if (_asset != null && _asset.SequenceEqual(t.Asset))
                            {
                                listStore.SetValue(iter, 0, t.Display);
                                break;
                            }
                        } while (listStore.IterNext(ref iter));
                    } catch 
                    {
                        Console.WriteLine("Exception in portfolio AssetMatadataChanged handler");
                    }
                });
            };
        }

        private void ConfigureList()
        {
            treeview1.Model = listStore;

            treeview1.RulesHint = true; //alternating colors
            treeview1.Selection.Mode = SelectionMode.Single;
            treeview1.Selection.Changed += OnSelectionChanged;
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

        void OnSelectionChanged(object sender, EventArgs e)
        {
        }

        void RenderCell(Gtk.TreeViewColumn column, Gtk.CellRenderer cellRenderer, Gtk.TreeModel model, Gtk.TreeIter iter)
        {
            var rowRenderer = cellRenderer as RowRenderer;

            rowRenderer.Asset = (string)model.GetValue(iter, 1);
            rowRenderer.Value = (decimal)model.GetValue(iter, 2);
        }

        public AssetDeltas AssetDeltas
        {
            get
            {
                return null;
            }
        }

        public void Clear()
        {
        }

        public void SetPortfolioDeltas(AssetDeltas assetDeltas)
        {
            foreach (var item in assetDeltas)
            {
                var value = item.Key.SequenceEqual(Consensus.Tests.zhash) ? new Zen(item.Value).Value : item.Value;

                listStore.Update(t=>t.SequenceEqual(item.Key), item.Key, App.Instance.Wallet.AssetsMetadata.GetMetadata(item.Key).Result, value);
            }
        }
    }
}

