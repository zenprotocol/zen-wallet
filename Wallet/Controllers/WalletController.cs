using System;
using System.Threading;
using Wallet.Domain;

namespace Wallet
{
	public class WalletController
	{
		private static WalletController instance = null;

		private ActionBarView _actionBarView;
		public ActionBarView ActionBarView {
			get {
				return _actionBarView;
			}
			set {
				_actionBarView = value;
				ActionBarView.Currency = "Zen";
			}
		}
		public TransactionsView TransactionsView { get; set; }

		public CurrencyEnum currency;
		public String currencyStr;

		private Thread tempThread;
		private bool stopping = false;

		public static WalletController GetInstance() {
			if (instance == null) {
				instance = new WalletController ();
			}

			return instance;
		}

		public WalletController ()
		{
			tempThread = new Thread (Reset);
			tempThread.Start ();

			EventBus.GetInstance ().Register ("button", delegate (String value) {
				switch (value) {
				case "Bitcoin":
					//currency = Wallet.Domain.TransactionItem.CurrencyEnum.BTC;
					ActionBarView.Currency = "Bitcoin";
					break;
				case "Ether":
					//currency = Wallet.Domain.TransactionItem.CurrencyEnum.ETH;
					ActionBarView.Currency = "Ether";
					break;
				case "Zen":
					//currency = Wallet.Domain.TransactionItem.CurrencyEnum.ZEN;
					ActionBarView.Currency = "Zen";
					break;
				case "Lite":
					//currency = Wallet.Domain.TransactionItem.CurrencyEnum.Lite;
					ActionBarView.Currency = "Lite";
					break;
				}
				Console.Write(value);
			});

		}

		public void Send(Decimal amount) {
			//UpdateUI ();
		}

		public void Quit() {
			stopping = true;
			tempThread.Join ();
		}

		private void Reset() {
			while (!stopping) {
				UpdateUI ();

				Thread.Sleep(1000);
			}
		}

		Random random = new Random();

		public void UpdateUI() {
			Gtk.Application.Invoke(delegate {
				if (ActionBarView != null) {
					//Alternative: Runtime.DispatchService.GuiDispatch (new StatefulMessageHandler (UpdateGui), n);
					ActionBarView.Total = random.Next(1, 13);
					ActionBarView.Rate = random.Next(1, 13);
				}

				if (TransactionsView != null) {

					DirectionEnum direcion = random.Next(0, 10) > 5 ? DirectionEnum.Sent : DirectionEnum.Recieved;
					Decimal amount = (Decimal)random.Next(1, 100000) / 1000000;

					TransactionsView.AddTransactionItem(
						new TransactionItem(amount, direcion)
					);
				}
			});
		}
	}
}

