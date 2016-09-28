using System;
using Gtk;

namespace Wallet
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			//TODO: will initializing MainController were have an effect on it's thread?

			Application.Init ();
			new WindowSSS(); // Refactoring is an impossible mission using MonoDevelop
			Application.Run ();
		}

		public static void CloseApp() {
		//	EventBus.GetInstance().Close();
			WalletController.GetInstance().Quit();
			Application.Quit ();
		}
	}
}
