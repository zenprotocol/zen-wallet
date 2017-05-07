using System;
using Gtk;
using System.Linq;
using Infrastructure;
using Wallet.core;
using NBitcoinDerive;

namespace NodeTester
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class TransactionsPane : Gtk.Bin
	{
		public TransactionsPane()
		{
			this.Build();

			InitKeysPane();
			InitTransactionsPane(treeviewTransactions);

			buttonKeyCreate.Clicked += ButtonKeyCreate_Clicked;
			buttonTransactionSend.Clicked += ButtonTransactionSend_Clicked;
			buttonMine.Clicked += ButtonMine_Clicked;
		}

		void ButtonTransactionSend_Clicked(object sender, EventArgs e)
		{
			try
			{
				var sendTo = entryTransactionSendTo.Text;
				var amount = UInt64.Parse(entryTransactionSendAmount.Text);

				String[] arr = sendTo.Split('-');
				byte[] sendToBytes = new byte[arr.Length];
				for (int i = 0; i < arr.Length; i++)
				{
					sendToBytes[i] = Convert.ToByte(arr[i], 16);
				}

		//		App.NodeManager.SendTransaction(sendToBytes, amount);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		void ButtonKeyCreate_Clicked(object sender, EventArgs e)
		{
		//	App.WalletManager.KeyStore.Add(new Wallet.core.Data.Key() { Change = true });
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

		private void InitTransactionsPane(TreeView treeView)
		{
			var store = new Gtk.ListStore(typeof(string));

			treeView.Model = store;
			treeView.AppendColumn("Amount", new Gtk.CellRendererText(), "text", 0);

			////TODO: resourceOwner.OwnResource(
			//MessageProducer<NodeManager.IMessage>.Instance.AddMessageListener(new EventLoopMessageListener<NodeManager.IMessage>(Message =>
			//{
			//	if (Message is NodeManager.TransactionAddToMempoolMessage)
			//	{
			//		NodeManager.TransactionAddToMempoolMessage transactionReceivedMessage = (NodeManager.TransactionAddToMempoolMessage) Message;

			//		Gtk.Application.Invoke(delegate
			//		{
			//			foreach (var output in transactionReceivedMessage.Transaction.outputs)
			//			{
			//				store.AppendValues(output.spend.amount.ToString());
			//			}
			//		});
			//	} else if (Message is NodeManager.TransactionAddToStoreMessage)
			//	{
			//		NodeManager.TransactionAddToStoreMessage transactionReceivedMessage = (NodeManager.TransactionAddToStoreMessage)Message;

			//		Gtk.Application.Invoke(delegate
			//		{
			//			foreach (var output in transactionReceivedMessage.Transaction.outputs)
			//			{
			//				store.AppendValues("store: " + output.spend.amount.ToString());
			//			}
			//		});
			//	}
			//}));
			////);
		}

		private void Populate(TreeView treeView, bool? used, bool? isChange)
		{
			//App.WalletManager.ListKeys(used, isChange).ToList().ForEach(key =>
			//{
			//	((ListStore) treeView.Model).AppendValues(
			//		DisplayKey(key.Address), 
			//		DisplayKey(key.Private), 
			//		key.Used ? "Yes" : "No", 
			//		key.Change ? "Yes" : "No"
			//	);
			//});
		}

		private String DisplayKey(byte[] key)
		{
			return key == null ? "" : System.Convert.ToBase64String(key);
		}

		void ButtonMine_Clicked(object sender, EventArgs e)
		{
			
		}
	}
}
