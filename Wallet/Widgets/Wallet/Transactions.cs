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
	public partial class Transactions : FocusableWidget, TransactionsView
	{
		private enum Columns {
			IsExpanded = 0,
			Data = 1
		}

		private ListStore listStore = new ListStore(
			typeof (bool), 
			typeof(TransactionItem)
		);
		
		private WalletController WalletController = WalletController.GetInstance ();

		TreeView treeView;
		public Transactions ()
		{
			this.Build ();

			WalletController.TransactionsView = this;

			ScrolledWindow sw = new ScrolledWindow();

			sw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			FindChild<Gtk.VBox>().PackStart(sw, true, true, 0);
			sw.Add(CreateList());
		}

		private TreeView CreateList() {
			treeView = new TreeView(listStore);

			treeView.RulesHint = true; //alternating colors
			treeView.Selection.Mode = SelectionMode.Single;
			treeView.HoverSelection = true;
			treeView.Selection.Changed += OnSelectionChanged;
			treeView.BorderWidth = 0;
			treeView.HeadersVisible = false;
			treeView.ModifyBase (Gtk.StateType.Normal, Constants.Colors.Base);

			Gtk.TreeViewColumn col = new Gtk.TreeViewColumn ();
			ExpandingCellRenderer rendered = new ExpandingCellRenderer();
			col.PackStart (rendered, true);
			col.SetCellDataFunc (rendered, new Gtk.TreeCellDataFunc (RenderCell));
			col.MinWidth = 130;
			treeView.AppendColumn (col);

			return treeView;
		}

		public override void Focus() {
			treeView.GrabFocus ();
		}

		void OnSelectionChanged(object sender, EventArgs e)
		{
			TreeIter selectionIter;
			TreeModel selectionModel;

			bool hasSelection = ((TreeSelection)sender).GetSelected (out selectionModel, out selectionIter);

			TreeIter storeIter;
			listStore.GetIterFirst (out storeIter);

			do {
				listStore.SetValue (storeIter, (int)Columns.IsExpanded, hasSelection && storeIter.Equals (selectionIter));
			} while (listStore.IterNext (ref storeIter));
		}
			
		private void RenderCell (Gtk.TreeViewColumn column, Gtk.CellRenderer cellRenderer, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			ExpandingCellRenderer expandedCellRenderer = cellRenderer as ExpandingCellRenderer;

			expandedCellRenderer.Expanded = (bool) model.GetValue (iter, (int) Columns.IsExpanded);
			expandedCellRenderer.TransactionItem = (TransactionItem) model.GetValue (iter, (int) Columns.Data);
		}

		public List<TransactionItem> TransactionsList { 
			set {
				foreach (TransactionItem transactionItem in value) {
					AddTransactionItem(transactionItem);
				}
			}
		}

		public void AddTransactionItem(TransactionItem transactionItem)
		{
			listStore.AppendValues(false, transactionItem);
		}
	}
}