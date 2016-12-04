using System;
using Gtk;
using System.Collections;
using System.Threading;

namespace Wallet
{
	class Program
	{
		public static Window temp; //TODO: remove

		public static void Main(string[] args)
		{
			App.Instance.Start();
		}

		public static void Close() {
			App.Instance.Close();
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