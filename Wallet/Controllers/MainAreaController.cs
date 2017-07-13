using System;
using Infrastructure;

namespace Wallet
{
	public class MainAreaController : Singleton<MainAreaController>
	{
		private const int DEFAULT_MENU_TOP_IDX = 0;

		private Type DEFAULT_CONTROL = typeof(LogLayout);//Wallet);

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
				case "Wallet":
					mainAreaView.Control = typeof(WalletLayout);
					break;
				case "Portfolio":
					mainAreaView.Control = typeof(Portfolio);
					break;
				case "History":
					mainAreaView.Control = typeof(Wallet);
					break;
				case "Contract":
					mainAreaView.Control = typeof(Contract);
					break;
				case "Balance":
					mainAreaView.Control = typeof(LogLayout);
					break;
				}
			}
		}
	}
}

