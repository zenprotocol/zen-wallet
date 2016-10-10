using System;
using Gtk;
using System.Collections;

namespace Wallet
{
	class Program
	{
		public static MainWindow MainWindow { get; private set; }

		public static void Main (string[] args)
		{
			//TODO: will initializing MainController were have an effect on it's thread?

			Application.Init ();
			MainWindow = new MainWindow();

//			Dialog dialog = new Dialog
//				("Sample", win, Gtk.DialogFlags.DestroyWithParent);
//			dialog.Modal = true;
//			dialog.AddButton ("Close", ResponseType.Close);
//		//	dialog.Response += new ResponseHandler (on_dialog_response);
//			dialog.Run ();
//			dialog.Destroy ();

			Application.Run ();
		}

		public static void CloseApp() {
		//	EventBus.GetInstance().Close();
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