using System;
using System.Collections.Generic;
using Gtk;
using Wallet.Domain;

namespace Wallet
{	
	public interface TransactionsView {
		List<TransactionItem> TransactionsList { set; }
		void AddTransactionItem (TransactionItem transaction);
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class Transactions : Gtk.Bin, TransactionsView
	{
		enum Column
		{
			Icon,
			Direction,
			Amount
		}

		private ListStore listStore = new ListStore(
			typeof (Gdk.Pixbuf), 
			typeof(String),
			typeof(int)
		);
		
		private WalletController WalletController = WalletController.GetInstance ();

		public Transactions ()
		{
			this.Build ();
			WalletController.TransactionsView = this;

			ScrolledWindow sw = new ScrolledWindow();
			sw.ShadowType = ShadowType.EtchedIn;
			sw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);

			((Gtk.VBox)Children [0]).PackStart(sw, true, true, 0);

//			InitStore();

			TreeView treeView = new TreeView(listStore);
			treeView.RulesHint = true;
			treeView.RowActivated += OnRowActivated;
			sw.Add(treeView);

			treeView.ModifyBase (Gtk.StateType.Normal, new Gdk.Color (0x01d, 0x025, 0x030));

			treeView.BorderWidth = 0;

			AddColumns(treeView);
		}

		void OnRowActivated (object sender, RowActivatedArgs args) {
			TreeIter iter;        
			TreeView view = (TreeView) sender;   

			if (view.Model.GetIter(out iter, args.Path)) {
//				string row = (string) view.Model.GetValue(iter, (int) Column.Type );
//				row += ", " + (string) view.Model.GetValue(iter, (int) Column.Amount );
//				row += ", " + view.Model.GetValue(iter, (int) Column.Year );
				//statusbar.Push(0, row);
			}
		}

		void AddColumns(TreeView treeView)
		{
			treeView.HeadersVisible = false;


			treeView.AppendColumn ("Icon", new Gtk.CellRendererPixbuf (), "pixbuf", 0);


			CellRendererText rendererText = new CellRendererText();
			TreeViewColumn column = new TreeViewColumn("Direction", rendererText,
				"text", Column.Direction);
			column.SortColumnId = (int) Column.Direction;
			treeView.AppendColumn(column);

			rendererText = new CellRendererText();
			column = new TreeViewColumn("Amount", rendererText, 
				"text", Column.Amount);
			column.SortColumnId = (int) Column.Amount;
			treeView.AppendColumn(column);

//			rendererText = new CellRendererText();
//			column = new TreeViewColumn("Currency", rendererText, 
//				"text", Column.Amount);
//			column.SortColumnId = (int) Column.Currency;
//			treeView.AppendColumn(column);
		}

		public List<TransactionItem> TransactionsList { 
			set {
				foreach (TransactionItem transactionItem in value) {
					AddTransactionItem(transactionItem);
				}
			}
		}

		Random random = new Random();

		public void AddTransactionItem(TransactionItem transactionItem)
		{
			listStore.AppendValues(
				Gdk.Pixbuf.LoadFromResource ("Wallet.Assets.misc." + (transactionItem.Direction == DirectionEnum.Sent ? "arrowup" : "arrowdown") + ".png"),
				transactionItem.Direction == DirectionEnum.Sent ? "Sent" : "Received", 
				//transactionItem.Amount
				random.Next(1, 100000) / 100 //WTF?!
			);
		}
	}
}