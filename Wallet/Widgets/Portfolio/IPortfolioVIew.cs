using System;
using Wallet.core;

namespace Wallet
{
    public interface IPortfolioVIew
    {
        void Clear();
        void SetPortfolioDeltas(AssetDeltas _AssetDeltas);
    }
}