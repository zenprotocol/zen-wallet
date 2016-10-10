using System;
using System.Collections.Generic;

namespace Wallet
{
	public interface IAssetsView
	{
		List<AssetType> Assets { set; }
	}
}

