using System;

namespace Wallet
{
	public class AssetType
	{
		public String Caption { get; set; }
		public String Image { get; set; }

		public AssetType(String caption, String image) {
			Caption = caption;
			Image = image;
		}
	}
}

