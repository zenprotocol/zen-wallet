using System;
using Gtk;
using System.Linq;
using Infrastructure;

namespace NodeTester
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class TransactionsPane : Gtk.Bin
	{
		public TransactionsPane()
		{
			this.Build();

			InitKeysPane();

			buttonKeyCreate.Clicked += ButtonKeyCreate_Clicked;
			buttonTransactionSend.Clicked += ButtonTransactionSend_Clicked;
		}

		void ButtonTransactionSend_Clicked(object sender, EventArgs e)
		{
			var sendTo = entryTransactionSendTo.Text;
			var amount = entryTransactionSendAmount.Text;

			WalletManager.Instance.SendTransaction();
		}

		void ButtonKeyCreate_Clicked(object sender, EventArgs e)
		{
			Wallet.core.Wallet.Instance.AddKey(new Wallet.core.Data.Key() { IsChange = true });
			Populate(treeviewKeysUnused, false, true);
		}

		private void InitKeysPane()
		{
			InitKeysPane(treeviewKeysUsed, true, false);
			InitKeysPane(treeviewKeysUnused, false, false);
			InitKeysPane(treeviewKeysChange, null, true);
		}

		private void InitKeysPane(TreeView treeView, bool? used, bool? isChange)
		{
			var store = new Gtk.ListStore(typeof(string), typeof(string), typeof(string), typeof(string));

			treeView.Model = store;
			treeView.AppendColumn("Public", new Gtk.CellRendererText(), "text", 0);
			treeView.AppendColumn("Private", new Gtk.CellRendererText(), "text", 1);
			treeView.AppendColumn("Used?", new Gtk.CellRendererText(), "text", 2);
			treeView.AppendColumn("Change?", new Gtk.CellRendererText(), "text", 3);

			Populate(treeView, used, isChange);
		}

		private void Populate(TreeView treeView, bool? used, bool? isChange)
		{
			Wallet.core.Wallet.Instance.GetKeys(used, isChange).ToList().ForEach(key =>
			{
				((ListStore) treeView.Model).AppendValues(DisplayKey(key.Public), DisplayKey(key.Private), key.Used ? "Yes" : "No", key.IsChange ? "Yes" : "No");
			});
		}

		private String DisplayKey(byte[] key)
		{
			return key == null ? "" : key.ToString();
		}
	}
}
