using System;
using System.Threading;
using Wallet.Domain;

namespace Wallet
{
	public class LogController
	{
		private static LogController instance = null;

		private Thread tempThread;
		private bool stopping = false;

		public ILogView LogView { get; set; }

		public static LogController GetInstance() {
			if (instance == null) {
				instance = new LogController ();
			}

			return instance;
		}
			
		public LogController ()
		{
			tempThread = new Thread (Reset);
			tempThread.Start ();
		}
			
		public void Quit() {
			stopping = true;
			tempThread.Join ();
		}

		private void Reset() {
			int i = 0;

			while (!stopping) {
				UpdateUI ();

				if (i++ > 26) {
					break;
				}

				Thread.Sleep(2000);
			}
		}

		Random random = new Random();

		public void UpdateUI() {
			Gtk.Application.Invoke(delegate {
				if (LogView != null) {
					LogView.AddLogEntryItem(new LogEntryItem(
						(Decimal)random.Next(1, 100000) / 1000000,
						random.Next(0, 10) > 5 ? DirectionEnum.Sent : DirectionEnum.Recieved,
						CurrencyEnum.ZEN,
						DateTime.Now.AddDays(-1 * random.Next(0, 100)),
						Guid.NewGuid().ToString("N"),
						Guid.NewGuid().ToString("N"),
						(Decimal)random.Next(1, 100) / 1000000
					));
				}
			});
		}
	}
}

