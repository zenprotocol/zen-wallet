using System;
using System.Linq;
using Gtk;
using Wallet.core;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class Portfolio : WidgetBase, IPortfolioVIew
	{
		ListStore listStore = new ListStore(
			typeof(byte[]),
			typeof(long)
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

			var x = new byte[Consensus.Tests.zhash.Length];
			Consensus.Tests.zhash.CopyTo(x, 0);
			x[x.Length - 1] = 0x01;
			listStore.AppendValues(x, 10);

			var y = new byte[Consensus.Tests.zhash.Length];
			Consensus.Tests.zhash.CopyTo(y, 0);
			y[x.Length - 1] = 0x02;
			listStore.AppendValues(y, 20);

			var z = new byte[Consensus.Tests.zhash.Length];
			Consensus.Tests.zhash.CopyTo(z, 0);
			z[x.Length - 1] = 0x03;
			listStore.AppendValues(z, 30);

			var t = new byte[Consensus.Tests.zhash.Length];
			Consensus.Tests.zhash.CopyTo(t, 0);
			t[x.Length - 1] = 0x04;
			listStore.AppendValues(t, 40);

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

		private void RenderCell(Gtk.TreeViewColumn column, Gtk.CellRenderer cellRenderer, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			var rowRenderer = cellRenderer as RowRenderer;

			rowRenderer.Asset = (byte[])model.GetValue(iter, 0);
			rowRenderer.Value = Convert.ToDecimal((long)model.GetValue(iter, 1) / Math.Pow(10, 8));
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

		public void SetDeltas(AssetDeltas assetDeltas)
		{
			foreach (var item in assetDeltas)
			{
				if (item.Key.SequenceEqual(Consensus.Tests.zhash))
				{
					labelZen.Text = String.Format(Constants.Formats.Money, item.Value / Math.Pow(10, 8)) + " " + App.Instance.Wallet.AssetsMetadata[item.Key];
				}
				else
				{
					listStore.AppendValues(item.Key, item.Value / Math.Pow(10, 8));
				}
			}
		}
	}
}

