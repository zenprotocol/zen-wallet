using System;
using Gtk;
using Wallet.Domain;
using System.Collections.Generic;
using Wallet.Constants;

namespace Wallet
{
	public interface ILogView {
		List<LogEntryItem> LogEntryList { set; }
		void AddLogEntryItem (LogEntryItem logEntryItem);
		void Totals(long sent, long recieved, long total);
		void Clear();
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class Log : WidgetBase, ILogView
	{
		ListStore logEntryStore = FactorStore ();
		ListStore logSummaryStore = FactorStore (new LogSummaryRow (0, 0, 0));

		private static ListStore FactorStore(ILogEntryRow logEntryRow = null) {
			ListStore listStore = new ListStore (typeof(ILogEntryRow));

			if (logEntryRow != null) {
				listStore.AppendValues (logEntryRow);
			}

			return listStore;
		}

		public Log ()
		{
			this.Build ();

			BalancesController.Instance.LogView = this;

			initList (listHeaders, FactorStore (new LogHeaderRow (0, Strings.Date, Strings.TransactionId, Strings.Sent, Strings.Received, Strings.Balance)));
			initList (listSummary, logSummaryStore);
			initList (listSummaryHeader, FactorStore (new LogHeaderRow (2, Strings.Sent, Strings.Received, Strings.Balance)));
			initList (listTransactions, logEntryStore);

			ExposeEvent += (object o, ExposeEventArgs args) => {
				listSummaryHeader.Hide ();
			};

			foreach (Widget w in new Widget[] { eventbox1, eventbox2, eventbox3, eventbox4, eventbox5, eventbox6, eventbox7 }) {
				w.ModifyBg (Gtk.StateType.Normal, Colors.Base.Gdk);
			}

			Adjustment adj1 = new Adjustment (0.0, 0.0, 101.0, 0.1, 1.0, 1.0);
		
			vscrollbar2.Adjustment = adj1;
			vscrollbar2.UpdatePolicy = UpdateType.Continuous;
			listTransactions.SetScrollAdjustments (null, adj1);

//			listTransactions.ScrollEvent += (object o, ScrollEventArgs args) => {
//				Console.WriteLine(args);
//			};
//			vscrollbar2.ChangeValue += (object o, ChangeValueArgs args) => {
//				Console.WriteLine (args);
//			};
//			vscrollbar2.MoveSlider += (object o, MoveSliderArgs args) => {
//				Console.WriteLine (args);
//			};
//			vscrollbar2.ScreenChanged += (object o, ScreenChangedArgs args) =>  {
//				Console.WriteLine(args);
//			};
//			vscrollbar2.AccelCanActivate += (object o, AccelCanActivateArgs args) => {
//				Console.WriteLine(args);
//			};
//			vscrollbar2.AccelClosuresChanged += (object sender, EventArgs e) => {
//				Console.WriteLine(e);
//			};
//			vscrollbar2.SizeRequested += (object o, SizeRequestedArgs args) => {
//				Console.WriteLine(args);
//			};
//			vscrollbar2.AdjustBounds += (object o, AdjustBoundsArgs args) => {
//				Console.WriteLine(args);
//			};
//
//
//			vscrollbar2.ConfigureEvent += (object o, ConfigureEventArgs args) => {
//				Console.WriteLine(args);
//			};
//			vscrollbar2.AccelCanActivate += (object o, AccelCanActivateArgs args) => {
//				Console.WriteLine(args);
//			};
//			vscrollbar2.AdjustBounds += (object o, AdjustBoundsArgs args) => {
//				Console.WriteLine(args);
//			};
//			vscrollbar2.ValueChanged += (object sender, EventArgs e) => {
//				Console.WriteLine(e);
//			};
//
//			adj1.Changed += (object sender, EventArgs e) => {
//				Console.WriteLine(":"+adj1.PageSize);
//			};
//			adj1.ValueChanged += (object sender, EventArgs e) => {
//				Console.WriteLine(e);
//			};
		
		}

		private void initList(TreeView list, ListStore store) {			
			list.Model = store;

			list.Selection.Mode = SelectionMode.None;		
			list.ModifyBase (Gtk.StateType.Normal, Colors.Base.Gdk);

			Gtk.TreeViewColumn col = new Gtk.TreeViewColumn ();

			LogCellRenderer renderer = new LogCellRenderer();

			col.PackStart (renderer, true);
			col.SetCellDataFunc (renderer, new Gtk.TreeCellDataFunc (
				(Gtk.TreeViewColumn column, Gtk.CellRenderer cellRenderer, Gtk.TreeModel model, Gtk.TreeIter iter) => {
					LogCellRenderer expandedCellRenderer = cellRenderer as LogCellRenderer;

					expandedCellRenderer.LogEntryRow = (ILogEntryRow) model.GetValue (iter, 0);
				})
			);

			list.AppendColumn (col);
		}

		public List<LogEntryItem> LogEntryList { 
			set {
				logEntryStore.Clear ();

				foreach (LogEntryItem logEntryItem in value) {
					AddLogEntryItem (logEntryItem);
				}
			}
		}

		public void Clear() { 
			logEntryStore.Clear();
		} 

		public void AddLogEntryItem (LogEntryItem logEntryItem) {			
			logEntryStore.AppendValues(new LogEntryRow(logEntryItem));
		}

		public void Totals(long sent, long recieved, long total)
		{
			TreeIter storeIter;
			logSummaryStore.GetIterFirst(out storeIter);

			var logSummaryRow = (LogSummaryRow)logSummaryStore.GetValue(storeIter, 0);

			logSummaryRow[0] = sent;
			logSummaryRow[1] = recieved;
			logSummaryRow[2] = total;

			logSummaryStore.SetValue(storeIter, 0, logSummaryRow);
		}
	}
}