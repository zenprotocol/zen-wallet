using System;
using Wallet.core;
using Wallet.Domain;
using System.Linq;
using Infrastructure;
using System.Collections.Generic;
using BlockChain.Data;

namespace Wallet
{
	public class PortfolioController : Singleton<PortfolioController>
	{
		List<IPortfolioVIew> _PortfolioViews = new List<IPortfolioVIew>();

		public PortfolioController()
		{
            App.Instance.Wallet.OnReset += delegate {
                UpdateViews();
            };
			//TODO: handle a single item, rather than redrawing the entire set
			App.Instance.Wallet.OnItems += delegate
			{
				UpdateViews();
			};
		}

		public void AddVIew(IPortfolioVIew view)
		{
			_PortfolioViews.Add(view);
            view.SetPortfolioDeltas(App.Instance.Wallet.TxDeltaList.AssetDeltas);
		}

		void UpdateViews()
		{
            Gtk.Application.Invoke(delegate
            {
                _PortfolioViews.ForEach(v =>
                {
	                v.Clear();
                    v.SetPortfolioDeltas(App.Instance.Wallet.TxDeltaList.AssetDeltas);
                });
			});
		}
	}
}