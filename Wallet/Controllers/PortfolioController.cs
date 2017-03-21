using System;
using Wallet.core;
using Wallet.Domain;
using System.Linq;
using Infrastructure;

namespace Wallet
{
	public class PortfolioController : Singleton<PortfolioController>
	{
		AssetDeltas _TxDeltas = new AssetDeltas();
		IPortfolioVIew _PortfolioView;

		public IPortfolioVIew PortfolioVIew
		{
			set
			{
				_PortfolioView = value;

				App.Instance.Wallet.TxDeltaList.ForEach(t => AddToTotals(t.AssetDeltas));
				App.Instance.Wallet.OnReset += delegate { value.Clear(); };
				App.Instance.Wallet.OnItems += a => a.ForEach(t => AddToTotals(t.AssetDeltas));
			}
		}

		void AddToTotals(AssetDeltas assetDeltas)
		{
			foreach (var item in assetDeltas)
			{
				if (!_TxDeltas.ContainsKey(item.Key))
					_TxDeltas[item.Key] = 0;

				_TxDeltas[item.Key] += item.Value;
			}

			UpdateTotals();
		}

		void UpdateTotals()
		{
			if (_PortfolioView == null)
				return;

			Gtk.Application.Invoke(delegate
			{
				_PortfolioView.SetDeltas(_TxDeltas);
			});
		}
	}
}

