using System;
using System.Threading;
using Wallet.Domain;

namespace Wallet
{
	public class WalletController
	{
		private const int DEFAULT_MENU_LEFT_IDX = 0;
		private static WalletController instance = null;

		public ITestTabsBarVertView TestTabsBarVertView { set { value.Default = DEFAULT_MENU_LEFT_IDX; } }

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

		public String CurrencySelected { 
			set {
				switch (value) {
					case "Bitcoin":
						currency = CurrencyEnum.BTC;
						ActionBarView.Currency = "Bitcoin";
						break;
					case "Ether":
						currency = CurrencyEnum.ETH;
						ActionBarView.Currency = "Ether";
						break;
					case "Zen":
						currency = CurrencyEnum.ZEN;
						ActionBarView.Currency = "Zen";
						break;
					case "Lite":
						currency = CurrencyEnum.LTE;
						ActionBarView.Currency = "Lite";
						break;
					}
					UpdateUI();
			}
		}

		public WalletController ()
		{
			tempThread = new Thread (Reset);
			tempThread.Start ();
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
					ActionBarView.Total = (decimal)random.Next(1, 1000) / 10000;
					ActionBarView.Rate = (decimal)random.Next(1, 1000) / 10000;
				}

				if (TransactionsView != null) {

					DirectionEnum direcion = random.Next(0, 10) > 5 ? DirectionEnum.Sent : DirectionEnum.Recieved;
					Decimal amount = (Decimal)random.Next(1, 100000) / 1000000;

					TransactionsView.AddTransactionItem(
						new TransactionItem(amount, direcion, currency)
					);
				}
			});
		}
	}
}

