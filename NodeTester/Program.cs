using System;
using Gtk;
using System.Threading;
using Infrastructure.TestingGtk;

namespace NodeTester
{
	public class MainClass
	{
		public static void Main (string[] args)
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
				App<MainWindow>.Create();
				Application.Run();
			}
		}
	}
}
