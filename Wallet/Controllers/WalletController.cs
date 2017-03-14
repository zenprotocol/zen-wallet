using System;
using Wallet.Domain;
using Wallet.core;
using Infrastructure;
using System.Linq;

namespace Wallet
{
	public class WalletController : Singleton<WalletController>
	{
		public ActionBarView ActionBarView { get; set; }
		AssetDeltas _AssetDeltas = new AssetDeltas();

		public ITransactionsView TransactionsView
		{
			set
			{
				Apply(value, App.Instance.Wallet.TxDeltaList);
				App.Instance.Wallet.OnReset += delegate { value.Clear(); };
				App.Instance.Wallet.OnItems += a => { Apply(value, a); };
			}
		}

		IWalletView _WalletView;
		public IWalletView WalletView { 
			get {
				return _WalletView;
			} 
			set {
				_WalletView = value;
				UpdateActionBar();
			} 
		}

		byte[] _Asset;
		public byte[] Asset
		{
			set
			{
				_Asset = value;

				AssetsMetadata assetsMetadata = App.Instance.Wallet.AssetsMetadata;
				AssetType = assetsMetadata[value];

				UpdateActionBar();
			}
		}

		public AssetType AssetType { get; private set; }

		public void Apply(ITransactionsView view, TxDeltaItemsEventArgs deltas)
		{
			Gtk.Application.Invoke(delegate
			{
				deltas.ForEach(u => u.AssetDeltas.ToList().ForEach(b => {
					if (!_AssetDeltas.ContainsKey(b.Key))
						_AssetDeltas[b.Key] = 0;	

					_AssetDeltas[b.Key] += b.Value;

					UpdateActionBar();

					view.AddTransactionItem(new TransactionItem(
					Math.Abs(b.Value),
					b.Value < 0 ? DirectionEnum.Sent : DirectionEnum.Recieved,
					App.Instance.Wallet.AssetsMetadata[b.Key],
					u.Time,
					Guid.NewGuid().ToString("N"),
					BitConverter.ToString(u.TxHash),
					0,
					u.TxState));
				}));
			});
		}

		private void UpdateActionBar()
		{
			bool hidden = AssetType is AssetTypeAll;

			if (WalletView == null)
				return;
			
			WalletView.ActionBar = !hidden;

			if (!hidden)
			{
				ActionBarView.Asset = AssetType;
				ActionBarView.Total = _AssetDeltas.ContainsKey(_Asset) ? _AssetDeltas[_Asset] : 0;
			}
		}
	}
}

