using System;
using Wallet.core;
using Wallet.Domain;
using System.Linq;
using Infrastructure;
using System.Collections.Generic;
using Gtk;

namespace Wallet
{
	public class AssetsController
	{
        IAssetsView _AssetsView;

		public AssetsController(IAssetsView assetsView)
		{
            _AssetsView = assetsView;

            App.Instance.AssetsMetadata.AssetMatadataChanged += AssetsMetadata_AssetMatadataChanged;

            _AssetsView.Assets = App.Instance.AssetsMetadata.GetAssetMatadataList();
		}

        void AssetsMetadata_AssetMatadataChanged(AssetMetadata assetMetadata)
		{
            Application.Invoke(delegate {
				_AssetsView.AssetUpdated = assetMetadata;
			});
		}
	}
}
