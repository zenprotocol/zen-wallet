using System;
using Wallet.core;
using Wallet.Domain;
using System.Linq;
using Infrastructure;

namespace Wallet
{
	public class BalancesController : Singleton<BalancesController>
	{
		AssetDeltas _AssetDeltasSent = new AssetDeltas();
		AssetDeltas _AssetDeltasRecieved = new AssetDeltas();
		AssetDeltas _AssetDeltasTotal = new AssetDeltas();
		TxDeltaItemsEventArgs _TxDeltas;
		ILogView _ILogView;

		public BalancesController()
		{
			Asset = Consensus.Tests.zhash;
		}

		public ILogView LogView
		{
			set
			{
				_ILogView = value;
				_TxDeltas = App.Instance.Wallet.TxDeltaList;
				Apply();
				App.Instance.Wallet.OnReset += delegate { value.Clear(); };
				App.Instance.Wallet.OnItems += a => { _TxDeltas = a; Apply(); };
			}
		}

		byte[] _Asset;
		public byte[] Asset
		{
			set
			{
				_Asset = value;

				if (_ILogView != null)
				{
					_ILogView.Clear();
					Apply();
				}
			}
		}

		void Apply()
		{
			_AssetDeltasSent.Clear();
			_AssetDeltasRecieved.Clear();
			_AssetDeltasTotal.Clear();

			Gtk.Application.Invoke(delegate
			{
				_TxDeltas.ForEach(u => u.AssetDeltas.Where(b => b.Key.SequenceEqual(_Asset)).ToList().ForEach(b =>
				{
					AddToTotals(b.Key, b.Value);

					_ILogView.AddLogEntryItem(new LogEntryItem(
					Math.Abs(b.Value),
					b.Value < 0 ? DirectionEnum.Sent : DirectionEnum.Recieved,
					App.Instance.Wallet.AssetsMetadata[b.Key],
					DateTime.Now,
					Guid.NewGuid().ToString("N"),
					Guid.NewGuid().ToString("N"),
					0
					));
				}));

				UpdateTotals();
			});
		}

		void AddToTotals(byte[] asset, long amount)
		{
			AddToTotals(amount < 0 ? _AssetDeltasSent : _AssetDeltasRecieved, asset, amount);
			AddToTotals(_AssetDeltasTotal, asset, amount);
		}

		void AddToTotals(AssetDeltas assetDeltas, byte[] asset, long amount)
		{
			if (!assetDeltas.ContainsKey(asset))
				assetDeltas[asset] = 0;

			assetDeltas[asset] += amount;
		}

		void UpdateTotals()
		{
			if (_ILogView == null)
				return;

			var sent = _AssetDeltasSent.ContainsKey(_Asset) ? _AssetDeltasSent[_Asset] : 0;
			var recieved = _AssetDeltasRecieved.ContainsKey(_Asset) ? _AssetDeltasRecieved[_Asset] : 0;
			var total = _AssetDeltasTotal.ContainsKey(_Asset) ? _AssetDeltasTotal[_Asset] : 0;

			_ILogView.Totals(sent, recieved, total);
		}
	}
}

