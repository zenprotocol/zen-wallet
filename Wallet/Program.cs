using System;
using Gtk;
using System.Collections;
using System.Threading;

namespace Wallet
{
	class Program
	{
		public static Window temp; //TODO: remove
		private static App _App;

		public static void Main(string[] args)
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
			_App = App.Create();
			Application.Run();
		
			//TODO: will initializing MainController were have an effect on it's thread?
		}

		public static void Close() {
			_App.Close();
			WalletController.GetInstance().Quit();
			LogController.GetInstance().Quit();
			Application.Quit ();
		}
	}
}
	
//TODO: rename interfaces
//TODO: handle memory leaks for pixbufs
//TODO: redesign scrollbars
//TODO: use namespaces