using System;
using System.Threading;
using System.Collections.Generic;

namespace Wallet
{
	public class MainAreaController
	{
		private const int DEFAULT_MENU_TOP_IDX = 1;
		private const int DEFAULT_MENU_LEFT_IDX = 1;

		private Type DEFAULT_CONTROL = typeof(Log);//Wallet);
		private static MainAreaController instance = null;

		private MainAreaView mainAreaView; 
		private IVerticalMenu verticalMenu;

		public MainAreaView MainAreaView { 
			set { 
				mainAreaView = value; 
				mainAreaView.Control = DEFAULT_CONTROL;
			} 
		}
	
		public MainView MainView { get; set; }

		public IMainMenuView MainMenuView { 
			set { 
				value.Default = DEFAULT_MENU_TOP_IDX; 
			} 
		}

		public IVerticalMenu VerticalMenuView { 
			private get { 
				return verticalMenu; 
			} 
			set { 
				verticalMenu = value; 
				value.Default = DEFAULT_MENU_LEFT_IDX; 
			} 
		}

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
					mainAreaView.Control = typeof(Portfolio);
					MainView.SideMenuVisible = false;
					break;
				case "Wallet":
					VerticalMenuView.AllVisible = true;
					mainAreaView.Control = typeof(Wallet);
					MainView.SideMenuVisible = true;
					break;
				case "Contract":
					mainAreaView.Control = typeof(Contract);
					MainView.SideMenuVisible = false;
					break;
				case "Balance":
					VerticalMenuView.AllVisible = false;
					mainAreaView.Control = typeof(Log);
					MainView.SideMenuVisible = true;
					break;
				}
			}
		}
	}
}

