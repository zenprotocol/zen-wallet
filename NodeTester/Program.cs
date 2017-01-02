using System;
using Gtk;
using System.Threading;
using Infrastructure.TestingGtk;
using NBitcoinDerive;
using Wallet.core;

namespace NodeTester
{
	public class MainClass
	{
		public static void Main (NodeManager nodeManager, WalletManager walletManager)
		{
			using (var consoleWriter = ConsoleMessage.Out)
			{
				GLib.ExceptionManager.UnhandledException += (GLib.UnhandledExceptionArgs e) =>
				{
					Console.WriteLine(e.ExceptionObject as Exception);
				};

				Thread.GetDomain().UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
				{
					Console.WriteLine(e.ExceptionObject as Exception);
				};

				Application.Init();
				App.Create(nodeManager, walletManager);
				Application.Run();
			}
		}
	}
}
