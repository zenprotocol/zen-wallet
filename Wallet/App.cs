using System;
using Infrastructure;
using Wallet.core;

namespace Wallet
{
	public class App : Singleton<App>
	{
		private Gtk.Window _MainWindow;
		public WalletManager Wallet { get; private set; }

		public App()
		{
			GLib.ExceptionManager.UnhandledException += (GLib.UnhandledExceptionArgs e) =>
			{
				Console.WriteLine(e.ExceptionObject as Exception);
			};

			System.Threading.Thread.GetDomain().UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
			{
				Console.WriteLine(e.ExceptionObject as Exception);
			};
		}

		public void Start(WalletManager walletManager)
		{
			Wallet = walletManager;

			Gtk.Application.Init();

			//using (var consoleWriter = ConsoleMessage.Out)
			//{
				_MainWindow = new MainWindow();
				DialogBase.parent = _MainWindow;
				_MainWindow.Show();
			//}

			Gtk.Application.Run();
		}

		public void Quit()
		{
			_MainWindow.Hide();
			Gtk.Application.Quit();
			//	a.RetVal = true;
			//	Hide();
		}
	}
}