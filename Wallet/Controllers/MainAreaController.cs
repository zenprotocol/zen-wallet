using System;
using Infrastructure;

namespace Wallet
{
	public class MainAreaController : Singleton<MainAreaController>
	{
		private const int DEFAULT_MENU_TOP_IDX = 1;

		private Type DEFAULT_CONTROL = typeof(Log);//Wallet);

		private IMainAreaView mainAreaView; 

		public IMainAreaView MainAreaView { 
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

		public void Spend() { }

		public String MainAreaSelected { 
			set {
			switch (value) {
				case "Portfolio":
					mainAreaView.Control = typeof(Portfolio);
					break;
				case "Wallet":
					mainAreaView.Control = typeof(Wallet);
					break;
				case "Contract":
					mainAreaView.Control = typeof(Contract);
					break;
				case "Balance":
					mainAreaView.Control = typeof(Log);
					break;
				}
			}
		}
	}
}

