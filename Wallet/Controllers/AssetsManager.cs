using System;
using System.Threading;
using System.Collections.Generic;

namespace Wallet
{
	public class AssetsManager
	{
		private static AssetsManager instance = null;

		private List<AssetType> assetsList = new List<AssetType> ();

		private AssetsManager() {
			assetsList.Add(new AssetType("Zen", "Zen"));
			assetsList.Add(new AssetType("Bitcoin", "Bitcoin"));
			assetsList.Add(new AssetType("Etherum", "Etherum"));
			assetsList.Add(new AssetType("Lite", "Litecoin"));
			assetsList.Add(new AssetType("Lite1", "Litecoin"));
			assetsList.Add(new AssetType("Lite2", "Litecoin"));
			assetsList.Add(new AssetType("Lite3", "Litecoin"));
			assetsList.Add(new AssetType("Lite4", "Litecoin"));
			assetsList.Add(new AssetType("Contracts", "Contracts"));
		}
	
		public void InitAssetsView(IAssetsView assetsView) { 
			assetsView.Assets = assetsList;
		}

		public static AssetsManager GetInstance() {
			if (instance == null) {
				instance = new AssetsManager ();
			}

			return instance;
		}
	}
}

