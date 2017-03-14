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
		TxDeltaItemsEventArgs _TxDeltas;
		ITransactionsView _ITransactionsView;

		public ITransactionsView TransactionsView
		{
			set
			{
				_ITransactionsView = value;

				Apply(App.Instance.Wallet.TxDeltaList);
				App.Instance.Wallet.OnReset += delegate { value.Clear(); };
				App.Instance.Wallet.OnItems += a => { Apply(a); };
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

				if (_ITransactionsView != null)
				{
					_ITransactionsView.Clear();
					Apply(_TxDeltas);
				}
			}
		}

		public AssetType AssetType { get; private set; }

		private void Apply(TxDeltaItemsEventArgs txDeltas)
		{
			_TxDeltas = txDeltas;
			_AssetDeltas.Clear();

			Gtk.Application.Invoke(delegate
			{
				_TxDeltas.ForEach(u => u.AssetDeltas.ToList().ForEach(b => {
					if (b.Key.SequenceEqual(_Asset))
					{
						if (!_AssetDeltas.ContainsKey(b.Key))
							_AssetDeltas[b.Key] = 0;

						_AssetDeltas[b.Key] += b.Value;

						UpdateActionBar();

						_ITransactionsView.AddTransactionItem(new TransactionItem(
							Math.Abs(b.Value),
							b.Value < 0 ? DirectionEnum.Sent : DirectionEnum.Recieved,
							App.Instance.Wallet.AssetsMetadata[b.Key],
							u.Time,
							Guid.NewGuid().ToString("N"),
							BitConverter.ToString(u.TxHash),
							0,
							u.TxState)
						 );
					}
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

