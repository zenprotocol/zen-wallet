using System;
using System.Threading;
using System.Collections.Generic;

namespace Wallet
{
	public class AssetsManager
	{
		private static AssetTypes assets = new AssetTypes();

		public static AssetTypes Assets { 
			get {
				return assets;
			}
		}
			
		static AssetsManager() {
			assets.Add(String.Empty, new AssetTypeAll());
			assets.Add("zen", new AssetType("Zen", "Zen"));
			assets.Add("key2", new AssetType("Bitcoin", "Bitcoin"));
			assets.Add("key3", new AssetType("Etherium", "Ether"));
			assets.Add("key4", new AssetType("Lite", "Litecoin"));
			assets.Add("key5", new AssetType("Lite1", "Litecoin"));
			assets.Add("key6", new AssetType("Lite2", "Litecoin"));
			assets.Add("key7", new AssetType("Lite3", "Litecoin"));
			assets.Add("key8", new AssetType("Lite4", "Litecoin"));
			assets.Add("key9", new AssetType("Contracts", "Contracts"));
		}
	}
}