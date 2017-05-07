using System;
using Infrastructure;
using NBitcoinDerive;
using Wallet.core;

namespace NodeTester
{
	public class App
	{
		public static NodeManager NodeManager { get ; set; }
		public static WalletManager WalletManager { get ; set; }

		public static App Create(NodeManager nodeManager, WalletManager walletManager)
		{
			NodeManager = nodeManager;
			WalletManager = walletManager;
			return new App();
		}

		private App()
		{
			MainWindow mainWindow = null;
			TryCatch(() => { mainWindow = new MainWindow(); mainWindow.Show(); }, e => ExceptionHandler(e));

		//	if (JsonLoader<Settings>.Instance.Value.AutoConfigure)
		//	{
		//	TryCatch(mainWindow, w => Runtime.Instance.Configure(w), (e, w) => ExceptionHandler(e, w));
		//	}
		}

		private void TryCatch(Action TryAction, Action<Exception> CatchAction)
		{
			try
			{
				TryAction();
			}
			catch (Exception e)
			{
				CatchAction(e);
			}
		}

		private void TryCatch(MainWindow mainWindow, Action<MainWindow> TryAction, Action<Exception, MainWindow> CatchAction)
		{
			try {
				TryAction(mainWindow);
			} catch (Exception e) {
				CatchAction(e, mainWindow);
			}
		}

		private void ExceptionHandler (Exception e, MainWindow mainWindow = null)
		{
			Console.WriteLine(e);

			try
			{
				Trace.Error("App", e);
			}
			catch (Exception traceExeption)
			{
				Console.WriteLine(traceExeption);
			}

			if (mainWindow != null)
			{
				try
				{
					mainWindow.ShowMessage($"App excption: {e.Message}");
				}
				catch (Exception showException)
				{
					Console.WriteLine(showException);
				}
			}
		}
	}
}