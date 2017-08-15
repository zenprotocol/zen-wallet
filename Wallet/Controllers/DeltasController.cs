using System;
using Wallet.core;
using Wallet.Domain;
using System.Linq;
using Infrastructure;
using System.Collections.Generic;
using BlockChain.Data;
using Gtk;

namespace Wallet
{
	public interface IDeltasVIew
	{
	}

	public class DeltasController
	{
        IDeltasVIew _DeltasVIew;
        readonly List<TxDelta> TxDeltaList = new List<TxDelta>();

		public DeltasController(IDeltasVIew deltasVIew)
		{
            _DeltasVIew = deltasVIew;

            AddTxDeltas(App.Instance.Wallet.TxDeltaList);

			App.Instance.Wallet.OnItems += AddTxDeltas;

            UpdateView();
		}

        void AddTxDeltas(List<TxDelta> txDeltas)
        {
            txDeltas.ForEach(AddTxDelta);

            Application.Invoke(delegate {
				UpdateView();
            });
        }

		void AddTxDelta(TxDelta txDelta)
		{
            TxDeltaList
                .Where(t => t.TxHash.SequenceEqual(txDelta.TxHash))
                .ToList()
                .ForEach(t => TxDeltaList.Remove(t));

			TxDeltaList.Add(txDelta);
		}

	    void UpdateView()
	    {
            if (_DeltasVIew is IPortfolioVIew)
            {
                ((IPortfolioVIew)_DeltasVIew).PortfolioDeltas = GetAssetTotals();
            }
            else if (_DeltasVIew is IStatementsVIew)
            {
                ((IStatementsVIew)_DeltasVIew).StatementsDeltas = TxDeltaList;
            }
	    }

		public AssetDeltas GetAssetTotals()
		{
			var assetDeltas = new AssetDeltas();

			TxDeltaList.ForEach(t =>
			{
				foreach (var item in t.AssetDeltas)
				{
					if (!assetDeltas.ContainsKey(item.Key))
						assetDeltas[item.Key] = 0;

					assetDeltas[item.Key] += item.Value;
				}
			});

			return assetDeltas;
		}
	}
}