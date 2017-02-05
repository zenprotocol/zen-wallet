using System;
using System.Collections.Generic;
using Gtk;
using Wallet.Domain;

namespace Wallet
{	
	public interface ITransactionsView {
		List<TransactionItem> TransactionsList { set; }
		void AddTransactionItem (TransactionItem transaction);
		void Clear();
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class Transactions : FocusableWidget, ITransactionsView
	{
		private bool setFocus = false;

		private enum Columns {
			IsExpanded = 0,
			Data = 1
		}

		private ListStore listStore = new ListStore(
			typeof (bool), 
			typeof(TransactionItem)
		);

		TreeView list;
		public Transactions ()
		{
			this.Build ();

			WalletController.Instance.SetTxView(this);

			ScrolledWindow sw = new ScrolledWindow();

			sw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			FindChild<Gtk.VBox>().PackStart(sw, true, true, 0);

			CreateList();

			sw.Add(list);
		}

		private void CreateList() {
			list = new TreeView(listStore);

			list.RulesHint = true; //alternating colors
			list.Selection.Mode = SelectionMode.Single;
			list.HoverSelection = true;
			list.Selection.Changed += OnSelectionChanged;
			list.BorderWidth = 0;
			list.HeadersVisible = false;
			list.ModifyBase (Gtk.StateType.Active, Constants.Colors.Base.Gdk);
			list.ModifyBase (Gtk.StateType.Selected, Constants.Colors.Base.Gdk);
			list.ModifyBase (Gtk.StateType.Normal, Constants.Colors.Base.Gdk);

			Gtk.TreeViewColumn col = new Gtk.TreeViewColumn ();
			ExpandingCellRenderer renderer = new ExpandingCellRenderer();
			col.PackStart (renderer, true);
			col.SetCellDataFunc (renderer, new Gtk.TreeCellDataFunc (RenderCell));
			col.MinWidth = 130;
			list.AppendColumn (col);
		}

		public override void Focus() {
//			TreeIter selectionIter;
//			setFocus = false;
//			if (list.Selection.GetSelected (out selectionIter)) {
//				list.GrabFocus ();
//				setFocus = true;
//			}
		}

		void OnSelectionChanged(object sender, EventArgs e)
		{
//			if (!setFocus) {
//				list.GrabFocus ();
//			}

			TreeIter selectionIter;

			bool hasSelection = ((TreeSelection)sender).GetSelected (out selectionIter);

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
				listStore.Clear ();

				foreach (TransactionItem transactionItem in value) {
					AddTransactionItem(transactionItem);
				}
			}
		}

		public void Clear()
		{
			listStore.Clear();
		}

		public void AddTransactionItem(TransactionItem transactionItem)
		{
			listStore.AppendValues(false, transactionItem);
		}
	}
}