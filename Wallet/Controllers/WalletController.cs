using System;
using Wallet.Domain;
using Wallet.core;
using Infrastructure;
using System.Linq;

namespace Wallet
{
	public class WalletController : Singleton<WalletController>
	{
		AssetDeltas _AssetDeltas = new AssetDeltas();
		TxDeltaItemsEventArgs _TxDeltas;
		ITransactionsView _ITransactionsView;

		public WalletController()
		{
			Asset = Consensus.Tests.zhash;
		}

		public ITransactionsView TransactionsView
		{
			set
			{
				_ITransactionsView = value;
				_TxDeltas = App.Instance.Wallet.TxDeltaList;
				Apply();

				App.Instance.Wallet.OnReset -= Wallet_OnReset; // ensure single registration
				App.Instance.Wallet.OnReset += Wallet_OnReset;
				App.Instance.Wallet.OnItems -= Wallet_OnItems; // ensure single registration
				App.Instance.Wallet.OnItems += Wallet_OnItems;
			}
		}

		void Wallet_OnReset(ResetEventArgs args)
		{
			_ITransactionsView.Clear();
			_TxDeltas = args.TxDeltaList;
			Apply();
		}

		void Wallet_OnItems(TxDeltaItemsEventArgs args)
		{
			_TxDeltas = args;
			Apply();
		}

		IWalletView _WalletView;
		public IWalletView WalletView { 
			get {
				return _WalletView;
			} 
			set {
				_WalletView = value;
				//UpdateActionBar();
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

				if (_ITransactionsView != null)
				{
					_ITransactionsView.Clear();
					Apply();
				}
			}
		}

		public AssetType AssetType { get; private set; }

		void Apply()
		{
			_AssetDeltas.Clear();

			Gtk.Application.Invoke(delegate
			{
				_TxDeltas.ForEach(u => u.AssetDeltas.Where(b => b.Key.SequenceEqual(_Asset)).ToList().ForEach(b => {
					if (!_AssetDeltas.ContainsKey(b.Key))
						_AssetDeltas[b.Key] = 0;

					_AssetDeltas[b.Key] += b.Value;

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
				}));

				//UpdateActionBar();
			});
		}

		//void UpdateActionBar()
		//{
		//	//bool hidden = AssetType is AssetTypeAll;

		//	if (WalletView == null)
		//		return;
			
		//	//WalletView.ActionBar = !hidden;

		//	//if (!hidden)
		//	//{
		//		ActionBarView.Asset = AssetType;
		//		ActionBarView.Total = _AssetDeltas.ContainsKey(_Asset) ? _AssetDeltas[_Asset] : 0;
		//	//}
		//}
	}
}

