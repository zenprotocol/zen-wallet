using System;
using Infrastructure;
using Infrastructure.TestingGtk;
using NodeTester;
using Wallet.core;

namespace Wallet
{
	public class App : Singleton<App>
	{
		private ResourceOwnerWindow _MainWindow;
		private INodeManager _Node;

		public App()
		{
			JsonLoader<NodeCore.Settings>.Instance.FileName = "NodeCore.Settings.json";
			JsonLoader<NodeTester.Settings>.Instance.FileName = "NodeTester.Settings.json";

			GLib.ExceptionManager.UnhandledException += (GLib.UnhandledExceptionArgs e) =>
			{
				Console.WriteLine(e.ExceptionObject as Exception);
			};

			System.Threading.Thread.GetDomain().UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
			{
				Console.WriteLine(e.ExceptionObject as Exception);
			};
		}

		public void Start()
		{
			Gtk.Application.Init();

			using (var consoleWriter = ConsoleMessage.Out)
			{
				_MainWindow = new MainWindow();
				Program.temp = _MainWindow; //TODO: remove
				_MainWindow.Show();

				_Node = new NodeManager();

				var consoleWindow = new ConsoleWindow(_MainWindow);

				consoleWindow.OnSettingsClicked += OpenSettings;
				consoleWindow.Show();

				JsonLoader<NodeTester.Settings>.Instance.OnSaved += StartNode;

				if (JsonLoader<NodeTester.Settings>.Instance.IsNew)
				{
					OpenSettings(); // temp
				}
				else
				{
					StartNode();
				}
			}

			Gtk.Application.Run();
		}

		public void OpenSettings()
		{
			new SettingsWindow();
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
		}
	}
}