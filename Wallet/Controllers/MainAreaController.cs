using System;
using System.Threading;

namespace Wallet
{
	public class MainAreaController
	{
		private const int DEFAULT_MENU_TOP_IDX = 1;
		private Type DEFAULT_CONTROL = typeof(Wallet);
		private static MainAreaController instance = null;

		private MainAreaView mainAreaView; 

		public MainAreaView MainAreaView { 
			set { 
				mainAreaView = value; 
				mainAreaView.Control = DEFAULT_CONTROL;
			} 
		}
	
		public MainView MainView { get; set; }
		public MainMenuView MainMenuView { set { value.Default = DEFAULT_MENU_TOP_IDX; } }

		public static MainAreaController GetInstance() {
			if (instance == null) {
				instance = new MainAreaController ();
			}

			return instance;
		}

		public String MainAreaSelected { 
			set {
			switch (value) {
				case "Portfolio":
					mainAreaView.Control = typeof(Wallet);
					MainView.SideMenuVisible = true;
					break;
				case "Wallet":
					mainAreaView.Control = typeof(Wallet);
					MainView.SideMenuVisible = true;
					break;
				case "Contract":
					mainAreaView.Control = typeof(Contract);
					MainView.SideMenuVisible = false;
					break;
				case "Log":
					mainAreaView.Control = typeof(Wallet);
					MainView.SideMenuVisible = false;
					break;
				}
			}
		}
	}
}

