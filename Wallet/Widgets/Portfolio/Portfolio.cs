using System;
using System.Collections.Generic;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class Portfolio : MenuBase, IAssetsView
	{
		public Portfolio ()
		{
			this.Build ();

			AssetsManager.GetInstance().InitAssetsView(this);
		}

		public List<AssetType> Assets { 
			set {
				foreach (AssetType assetType in value) {
				//	PortfolioOther portfolioOther = new PortfolioOther ();

				//	vboxOthers.PackStart(portfolioOther, true, true, 0);;
				}
			} 
		}
	}
}

