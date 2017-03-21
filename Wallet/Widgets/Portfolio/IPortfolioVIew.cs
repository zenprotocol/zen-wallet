using System;
using Wallet.core;

namespace Wallet
{
	public interface IPortfolioVIew
	{
		void Clear();
		void SetDeltas(AssetDeltas _AssetDeltas);
	}
}