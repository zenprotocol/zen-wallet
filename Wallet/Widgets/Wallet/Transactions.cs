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
			typeof(String)
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

//			TreeViewColumn column;
//
//			column = new TreeViewColumn("Icon", new CellRendererPixbuf(),
//				"pixbuf", Column.Icon);
//			column.MinWidth = 100;
//	//		treeView.AppendColumn ("Icon", new Gtk.CellRendererPixbuf (), "pixbuf", 0);
//			treeView.AppendColumn(column);
//
//			CellRendererText rendererText = new CellRendererText();
//			column = new TreeViewColumn("Direction", rendererText,
//				"text", Column.Direction);
//			column.SortColumnId = (int) Column.Direction;
//			column.MinWidth = 200;
//			treeView.AppendColumn(column);
//
//			rendererText = new CellRendererText();
//			column = new TreeViewColumn("Amount", rendererText, 
//				"text", Column.Amount);
//			column.SortColumnId = (int) Column.Amount;
//
//			treeView.AppendColumn(column);

//			rendererText = new CellRendererText();
//			column = new TreeViewColumn("Currency", rendererText, 
//				"text", Column.Amount);
//			column.SortColumnId = (int) Column.Currency;
//			treeView.AppendColumn(column);




			Gtk.TreeViewColumn col0 = new Gtk.TreeViewColumn ();
			Gtk.CellRendererPixbuf renderCol0 = new Gtk.CellRendererPixbuf ();
			col0.PackStart (renderCol0, true);
			col0.SetCellDataFunc (renderCol0, new Gtk.TreeCellDataFunc (RenderCellCol0));
			col0.MinWidth = 130;
			treeView.AppendColumn (col0);

			Gtk.TreeViewColumn col1 = new Gtk.TreeViewColumn ();
			Gtk.CellRendererText renderCol1 = new Gtk.CellRendererText ();
			col1.PackStart (renderCol1, true);
			col1.SetCellDataFunc (renderCol1, new Gtk.TreeCellDataFunc (RenderCellCol1));
			col1.MinWidth = 130;
			treeView.AppendColumn (col1);


			Gtk.TreeViewColumn col2 = new Gtk.TreeViewColumn ();
			Gtk.CellRendererText renderCol2 = new Gtk.CellRendererText ();
			col2.PackStart (renderCol2, true);
			col2.SetCellDataFunc (renderCol2, new Gtk.TreeCellDataFunc (RenderCellCol2));
			treeView.AppendColumn (col2);

			treeView.Model = listStore;


		}


		private void RenderCellCol0 (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			Gdk.Pixbuf img = (Gdk.Pixbuf) model.GetValue (iter, 0);
			(cell as Gtk.CellRendererPixbuf).Pixbuf = img;
		}


		private void RenderCellCol1 (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			String value = (String) model.GetValue (iter, 1);
			(cell as Gtk.CellRendererText).Foreground = "white";
			(cell as Gtk.CellRendererText).Text = value;
			(cell as Gtk.CellRendererText).FontDesc = Pango.FontDescription.FromString ("Aharoni CLM 12");
		}

		private void RenderCellCol2 (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			String value = (String) model.GetValue (iter, 2);
			(cell as Gtk.CellRendererText).Foreground = "white";
			(cell as Gtk.CellRendererText).Text = value + " ZEN";
			(cell as Gtk.CellRendererText).FontDesc = Pango.FontDescription.FromString ("Aharoni CLM 12");
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
				transactionItem.Amount.ToString()
			);
		}
	}
}