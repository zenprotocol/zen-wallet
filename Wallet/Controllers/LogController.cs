using System;
using Wallet.core;
using Wallet.Domain;
using System.Linq;

namespace Wallet
{
	public class LogController
	{
		private static LogController instance = null;
		public ILogView LogView { get; set; }

		public static LogController GetInstance() {
			if (instance == null) {
				instance = new LogController ();
			}

			return instance;
		}
			
		public LogController ()
		{
			App.Instance.Wallet.OnNewTransaction += HandleNewTransaction;
		}

		public void Sync()
		{
			LogView.Clear();

			foreach (var transactionSpendData in App.Instance.Wallet.MyTransactions)
			{
				HandleNewTransaction(transactionSpendData);
			}
		}

		public void HandleNewTransaction(TransactionSpendData transactionSpendData) {
			Gtk.Application.Invoke(delegate {
				if (LogView != null) {
					foreach (var item in transactionSpendData.Balances)
					{
						var asset = item.Key;
						var amount = item.Value;

						LogView.AddLogEntryItem(new LogEntryItem(
							amount,
							amount < 0 ? DirectionEnum.Sent : DirectionEnum.Recieved,
							AssetsHelper.Find(asset),
							DateTime.Now,
							Guid.NewGuid().ToString("N"),
							Guid.NewGuid().ToString("N"),
							amount
						));
					}
				}
			});
		}
	}
}

