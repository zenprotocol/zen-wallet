//using System;
//using System.Linq;
//using Consensus;

//namespace Wallet
//{
//	public class AssetsManager
//	{
//		private static AssetTypes assetTypes = new AssetTypes();
//		private static AssetCodes assetCodes = new AssetCodes();

//		public static AssetTypes AssetTypes
//		{
//			get
//			{
//				return assetTypes;
//			}
//		}

//		public static AssetCodes AssetCodes
//		{
//			get
//			{
//				return assetCodes;
//			}
//		}

//		public static AssetType Find(byte[] code)
//		{
//			foreach (var item in AssetCodes)
//			{
//				if (item.Value.SequenceEqual(code))
//				{
//					return assetTypes[item.Key];
//				}
//			}

//			return null;
//		}
			
//		static AssetsManager() {
//			assetTypes.Add(String.Empty, new AssetTypeAll());
//			assetTypes.Add("zen", new AssetType("Zen", "Zen"));

//			assetCodes.Add("zen", Tests.zhash);
//			//assetTypes.Add("key2", new AssetType("Bitcoin", "Bitcoin"));
//			//assetTypes.Add("key3", new AssetType("Etherium", "Ether"));
//			//assetTypes.Add("key4", new AssetType("Lite", "Litecoin"));
//			//assetTypes.Add("key5", new AssetType("Lite1", "Litecoin"));
//			//assetTypes.Add("key6", new AssetType("Lite2", "Litecoin"));
//			//assetTypes.Add("key7", new AssetType("Lite3", "Litecoin"));
//			//assetTypes.Add("key8", new AssetType("Lite4", "Litecoin"));
//			//assetTypes.Add("key9", new AssetType("Contracts", "Contracts"));
//		}
//	}
//}