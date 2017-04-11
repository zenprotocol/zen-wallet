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
		ILogView _LogView;

		public BalancesController()
		{
			Asset = Consensus.Tests.zhash;
		}

		public ILogView LogView
		{
			set
			{
				_LogView = value;
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

				if (_LogView != null)
				{
					_LogView.Clear();
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

					var total = _AssetDeltasTotal.ContainsKey(_Asset) ? _AssetDeltasTotal[_Asset] / Math.Pow(10, 8) : 0;
					var decTotal = Convert.ToDecimal(total);

					_LogView.AddLogEntryItem(new LogEntryItem(
					Math.Abs(b.Value),
					b.Value < 0 ? DirectionEnum.Sent : DirectionEnum.Recieved,
					App.Instance.Wallet.AssetsMetadata[b.Key],
					u.Time,
					"TODO",
					BitConverter.ToString(u.TxHash),
					decTotal
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
			if (_LogView == null)
				return;

			var sent = _AssetDeltasSent.ContainsKey(_Asset) ? _AssetDeltasSent[_Asset] / Math.Pow(10, 8) : 0;
			var recieved = _AssetDeltasRecieved.ContainsKey(_Asset) ? _AssetDeltasRecieved[_Asset] / Math.Pow(10, 8) : 0;
			var total = _AssetDeltasTotal.ContainsKey(_Asset) ? _AssetDeltasTotal[_Asset] / Math.Pow(10, 8) : 0;

			_LogView.Totals(Convert.ToDecimal(sent), Convert.ToDecimal(recieved), Convert.ToDecimal(total));
		}
	}
}

