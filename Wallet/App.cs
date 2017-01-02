using System;
using Infrastructure;
using Infrastructure.TestingGtk;
using Wallet.core;
using NBitcoinDerive;

namespace Wallet
{
	public class App : Singleton<App>
	{
		private ResourceOwnerWindow _MainWindow;
		public NodeManager NodeManager { get; private set; }
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

		public void Start(NodeManager nodeManager, WalletManager walletManager)
		{
			NodeManager = nodeManager;
			Wallet = walletManager;

			Gtk.Application.Init();

			using (var consoleWriter = ConsoleMessage.Out)
			{
				_MainWindow = new MainWindow();
		//		Program.temp = _MainWindow; //TODO: remove
				_MainWindow.Show();

		//		_Node = new NodeManager();

		//		var consoleWindow = new ConsoleWindow(_MainWindow);

		//		consoleWindow.OnSettingsClicked += OpenSettings;
		//		consoleWindow.Show();

				//JsonLoader<NodeTester.Settings>.Instance.OnSaved += StartNode;

				//if (JsonLoader<NodeTester.Settings>.Instance.IsNew)
				//{
				//	OpenSettings(); // temp
				//}
				//else
				//{
				//	StartNode();
				//}
			}

			Gtk.Application.Run();
		}

		public void OpenSettings()
		{
	//		new SettingsWindow();
		}

		private async void StartNode()
		{
//#if DEBUG
//			if (JsonLoader<NodeTester.Settings>.Instance.Value.DowngradeToLAN)
//			{
//				_Node = new LanNodeManager();
//			}
//#endif
	//		await _Node.Start(_MainWindow, NodeCore.TestNetwork.Instance);
		}

		public void Close()
		{
			_MainWindow.Dispose();
			WalletController.GetInstance().Quit();
			LogController.GetInstance().Quit();
			NodeManager.Dispose ();
			Gtk.Application.Quit ();
		}
	}
}