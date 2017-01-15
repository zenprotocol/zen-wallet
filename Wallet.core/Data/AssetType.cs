using System;

namespace Wallet.core
{
	public class AssetType
	{
		public String Caption { get; set; }
		public String Image { get; set; }

		public AssetType(String caption, String image) {
			Caption = caption;
			Image = image;
		}

		public override string ToString()
		{
			return Caption;
		}
	}

	public class AssetTypeAll : AssetType
	{
		public AssetTypeAll() : base("All", "All") {}
	}

}

