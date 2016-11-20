using System;
using Gtk;
using Infrastructure;
using NodeCore;

namespace NodeTester
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class TransactionsPane : Gtk.Bin
	{
		public TransactionsPane()
		{
			this.Build();

			InitKeysPane();
		}

		private void InitKeysPane()
		{
			InitKeysPane(treeviewKeysUsed);
			InitKeysPane(treeviewKeysUnused);
			InitKeysPane(treeviewKeysChange);
		}

		private void InitKeysPane(TreeView treeView)
		{
			var store = new Gtk.ListStore(typeof(string), typeof(string));

			treeView.AppendColumn("Public", new Gtk.CellRendererText(), "text", 0);
			treeView.AppendColumn("Private", new Gtk.CellRendererText(), "text", 1);
			treeView.AppendColumn("Used?", new Gtk.CellRendererText(), "text", 1);
			treeView.AppendColumn("Change?", new Gtk.CellRendererText(), "text", 1);

			//

			treeView.Model = store;
		}
	}
}
