using System;
using Gtk;

namespace Wallet
{
	class Program
	{
		public static void Main (string[] args)
		{
			//TODO: will initializing MainController were have an effect on it's thread?

			Application.Init ();
			new MainWindow();
		//	new Driver1 ();
			Application.Run ();
		}

		public static void CloseApp() {
		//	EventBus.GetInstance().Close();
			WalletController.GetInstance().Quit();
			Application.Quit ();
		}
	}
}
