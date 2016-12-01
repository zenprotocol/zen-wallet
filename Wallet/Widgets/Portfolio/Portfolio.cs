using System;
using System.Collections.Generic;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class Portfolio : MenuBase
	{
		public Portfolio ()
		{
			this.Build ();

//			AssetsManager.GetInstance().InitAssetsView(this);
		}

//		public AssetTypes Assets { 
//			set {
//				foreach (AssetType assetType in value.Values) {
//				//	PortfolioOther portfolioOther = new PortfolioOther ();
//
//				//	vboxOthers.PackStart(portfolioOther, true, true, 0);;
//				}
//			} 
//		}
	}
}

