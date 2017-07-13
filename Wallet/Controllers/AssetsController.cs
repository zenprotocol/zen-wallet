using System;
using Wallet.core;
using Wallet.Domain;
using System.Linq;
using Infrastructure;

namespace Wallet
{
	public class AssetsController
	{
        IAssetsView _AssetsView;

		public AssetsController(IAssetsView assetsView)
		{
            _AssetsView = assetsView;

			App.Instance.Wallet.AssetsMetadata.AssetMatadataChanged -= AssetsMetadata_AssetMatadataChanged;
			App.Instance.Wallet.AssetsMetadata.AssetMatadataChanged += AssetsMetadata_AssetMatadataChanged;

			_AssetsView.Assets = App.Instance.Wallet.AssetsMetadata.GetAssetMatadataList();
		}

		void AssetsMetadata_AssetMatadataChanged(AssetMetadata assetMetadata)
		{
            _AssetsView.AssetUpdated = assetMetadata;
		}
	}
}
