using System;
using Gtk;
using Wallet.Domain;
using System.Linq;
using Wallet.Constants;
using Wallet.core;
using System.Collections.Generic;

namespace Wallet
{
	public interface IStatementsVIew : IDeltasVIew
	{
        List<TxDelta> StatementsDeltas { set; }
	}

    [System.ComponentModel.ToolboxItem(true)]
    public partial class Log : WidgetBase, IStatementsVIew
    {
        ListStore logEntryStore = new ListStore(typeof(LogEntryItem));
        List<TxDelta> _TxDeltas;
        byte[] _SelectedAsset;

        static ListStore FactorStore(ILogEntryRow logEntryRow = null)
        {
            ListStore listStore = new ListStore(typeof(ILogEntryRow));

            if (logEntryRow != null)
            {
                listStore.AppendValues(logEntryRow);
            }

            return listStore;
        }

        public List<TxDelta> StatementsDeltas
        {
            set { _TxDeltas = value; UpdateView(); }
        }

		public byte[] SelectedAsset
		{
            set { _SelectedAsset = value; UpdateView(); }
		}

        public Log()
        {
            this.Build();

            new DeltasController(this);

            InitList(listHeaders, FactorStore(new LogHeaderRow(Strings.Date, Strings.TransactionId, Strings.Sent, Strings.Received, Strings.Balance)), new LogHeaderRenderer());
            InitList(listTransactions, logEntryStore, new LogEntryRenderer());

            listTransactions.RulesHint = true; //alternating colors

			foreach (Widget w in new Widget[] { eventbox1, eventbox4, eventbox7 })
            {
                w.ModifyBg(Gtk.StateType.Normal, Colors.Base.Gdk);
            }

            Adjustment adj1 = new Adjustment(0.0, 0.0, 101.0, 0.1, 1.0, 1.0);

            vscrollbar2.Adjustment = adj1;
            vscrollbar2.UpdatePolicy = UpdateType.Continuous;
            listTransactions.SetScrollAdjustments(null, adj1);
        }

        private void InitList(TreeView list, ListStore store, LogRendererBase renderer)
        {
            list.Model = store;

            list.Selection.Mode = SelectionMode.None;
            list.ModifyBase(Gtk.StateType.Normal, Colors.Base.Gdk);

            Gtk.TreeViewColumn col = new Gtk.TreeViewColumn();

            col.PackStart(renderer, true);
            col.SetCellDataFunc(renderer, new Gtk.TreeCellDataFunc(
                (Gtk.TreeViewColumn column, Gtk.CellRenderer cellRenderer, Gtk.TreeModel model, Gtk.TreeIter iter) =>
                {
                    if (cellRenderer is LogEntryRenderer)
                    {
                        var logEntryRenderer = (LogEntryRenderer)cellRenderer;
                        
                       logEntryRenderer.LogEntryItem = (LogEntryItem)model.GetValue(iter, 0);
                    }
				
				})
            );

            list.AppendColumn(col);
        }

        void UpdateView()
        {
            logEntryStore.Clear();

            long runningBalance = 0;

            if (_TxDeltas != null && _SelectedAsset != null)
                _TxDeltas.ForEach(
                    txDelta => txDelta.AssetDeltas.Where(
                        assetDelta => assetDelta.Key.SequenceEqual(_SelectedAsset)).ToList().ForEach(
                            assetDelta =>
                            {
                                runningBalance += assetDelta.Value;
                                
                                var absValue = (ulong)Math.Abs(assetDelta.Value);

                                logEntryStore.AppendValues(new LogEntryItem(
                                    absValue,
                                    assetDelta.Value < 0 ? DirectionEnum.Sent : DirectionEnum.Recieved,
                                    assetDelta.Key,
                                    txDelta.Time,
                                    Convert.ToBase64String(txDelta.TxHash),
                                    txDelta.TxState.ToString(),
                                    runningBalance));
                            }));
        }
    }
}