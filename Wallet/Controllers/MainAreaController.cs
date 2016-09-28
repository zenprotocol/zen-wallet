using System;
using System.Threading;

namespace Wallet
{
	public class MainAreaController
	{
		private const int DEFAULT_MENU_TOP_IDX = 1;
		private static MainAreaController instance = null;

		private MainAreaView mainAreaView; 

		public MainAreaView MainAreaView { 
			set { 
				mainAreaView = value; mainAreaView.Page = DEFAULT_MENU_TOP_IDX; 
			} 
		}
	
		public MainView MainView { get; set; }
		public TestTabsBarView TestTabsBarView { set { value.Default = DEFAULT_MENU_TOP_IDX; } }

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
						mainAreaView.Page = 0;
						MainView.SideMenuVisible = true;
						break;
					case "Wallet":
						mainAreaView.Page = 1;
						MainView.SideMenuVisible = true;
						break;
					case "Contract":
						mainAreaView.Page = 2;
						MainView.SideMenuVisible = false;
						break;
					}
				}
		}
	}
}

