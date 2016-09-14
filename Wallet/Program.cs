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
			MainWindow win = new MainWindow ();
			win.Show ();
			Application.Run ();
		}
	}
}
