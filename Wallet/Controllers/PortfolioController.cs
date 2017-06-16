using System;
using Wallet.core;
using Wallet.Domain;
using System.Linq;
using Infrastructure;
using System.Collections.Generic;
using BlockChain.Data;

namespace Wallet
{
    public class AggregatingAssetDeltas : BlockChain.Data.HashDictionary<List<Tuple<long, TxStateEnum>>>
    {
    }

	public class PortfolioController : Singleton<PortfolioController>
	{
		AssetDeltas _TxDeltas = new AssetDeltas();
		List<IPortfolioVIew> _PortfolioViews = new List<IPortfolioVIew>();

		public PortfolioController()
		{
			App.Instance.Wallet.TxDeltaList.ForEach(t => 
			    AddToTotals(t.AssetDeltas)
			);
			App.Instance.Wallet.OnReset += delegate { 
                _PortfolioViews.ForEach(v => v.Clear()); 
            };
			App.Instance.Wallet.OnItems += a => { 
                a.ForEach(t => AddToTotals(t.AssetDeltas)); UpdateTotals(); 
            };
		}

		public void AddVIew(IPortfolioVIew view)
		{
			_PortfolioViews.Add(view);
            view.SetPortfolioDeltas(_TxDeltas);
		}

		void AddToTotals(AssetDeltas assetDeltas)
		{
			foreach (var item in assetDeltas)
			{
				if (!_TxDeltas.ContainsKey(item.Key))
					_TxDeltas[item.Key] = 0;

				_TxDeltas[item.Key] += item.Value;
			}
		}

		void UpdateTotals()
		{
			Gtk.Application.Invoke(delegate
			{
				_PortfolioViews.ForEach(v => v.SetPortfolioDeltas(_TxDeltas));
			});
		}
	}
}