using System;
using Wallet.core;
using Wallet.Domain;
using System.Linq;
using Infrastructure;
using System.Collections.Generic;

namespace Wallet
{
	public class AssetsController
	{
        IAssetsView _AssetsView;

		public AssetsController(IAssetsView assetsView)
		{
            _AssetsView = assetsView;

			AssetsMetadata.Instance.AssetMatadataChanged += AssetsMetadata_AssetMatadataChanged;

			_AssetsView.Assets = AssetsMetadata.Instance.GetAssetMatadataList();
		}

        void AssetsMetadata_AssetMatadataChanged(AssetMetadata assetMetadata)
		{
            _AssetsView.AssetUpdated = assetMetadata;
		}
	}
}
