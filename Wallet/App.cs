using System;
using Infrastructure;
using Infrastructure.TestingGtk;
using NodeCore;
using NodeTester;

namespace Wallet
{
	public class App
	{
		private ResourceOwnerWindow _MainWindow;

		private NodeManager _Node;

		static App() {
			JsonLoader<NodeCore.Settings>.Instance.FileName = "NodeCore.Settings.json";
			JsonLoader<NodeTester.Settings>.Instance.FileName = "NodeTester.Settings.json";
		}

		public static App Create()
		{
			return new App();
		}

		private App()
		{
			using (var consoleWriter = ConsoleMessage.Out)
			{

				_MainWindow = new MainWindow();
				Program.temp = _MainWindow; //TODO: remove
				_MainWindow.Show();

				_Node = new NodeManager();

				var consoleWindow = new ConsoleWindow();

				consoleWindow.DestroyEvent += (o, args) =>
				{
					consoleWindow = null;
				//	DisposeResources(); //WTF??
				};

				consoleWindow.Show();

				JsonLoader<NodeTester.Settings>.Instance.OnSaved += async () =>
				{
					await _Node.Start(_MainWindow);
				};

				if (JsonLoader<NodeTester.Settings>.Instance.IsNew)
				{
					new SettingsWindow().Show();
				}
				else
				{
					_Node.Start(_MainWindow);
				}
			}
		}

		public void Close()
		{
			_MainWindow.Dispose();
		}
	}
}