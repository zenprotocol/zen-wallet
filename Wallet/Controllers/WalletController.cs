using System;
using Wallet.Domain;
using Wallet.core;
using BlockChain.Data;
using System.Collections.Generic;

namespace Wallet
{
	public class WalletController
	{
		private static WalletController instance = null;

		private ActionBarView _actionBarView;
		public ActionBarView ActionBarView {
			get {
				return _actionBarView;
			}
			set {
				_actionBarView = value;
			//	ActionBarView.Asset = CurrencyEnum.Zen;
			}
		}

		public SendStub SendStub = new SendStub ();

		public TransactionsView TransactionsView { get; set; }
		public IWalletView WalletView { get; set; }

		private AssetType asset = AssetsHelper.AssetTypes["zen"];

		public static WalletController GetInstance() {
			if (instance == null) {
				instance = new WalletController ();
			}

			return instance;
		}

		public AssetType Asset { 
			set {
				asset = value;
				ActionBarView.Asset = value;
				WalletView.ActionBar = !(value is AssetTypeAll);
			} get {
				return asset;
			}
		}

		public WalletController()
		{
			App.Instance.Wallet.OnNewBalance += OnNewBalance;
		}

		public void Load()
		{
			TransactionsView.Clear();
			OnNewBalance(App.Instance.Wallet.Load());
		}

		public void OnNewBalance(HashDictionary<List<long>> balances)
		{
			//if (ActionBarView != null)
			//{
			//	//Alternative: Runtime.DispatchService.GuiDispatch (new StatefulMessageHandler (UpdateGui), n);
			//	ActionBarView.Total = (decimal)10000;
			//	ActionBarView.Rate = (decimal)10000;
			//}
			Gtk.Application.Invoke(delegate
			{
				if (TransactionsView != null)
				{
					foreach (var item in balances)
					{
						var asset = item.Key;

						foreach (var item_ in item.Value)
						{
							var amount = item.Value;

							TransactionsView.AddTransactionItem(new TransactionItem(
								(ulong)Math.Abs(item_),
								item_ < 0 ? DirectionEnum.Sent : DirectionEnum.Recieved,
								AssetsHelper.Find(asset),
								DateTime.Now,
								Guid.NewGuid().ToString("N"),
								Guid.NewGuid().ToString("N"),
								0
							));
						}
					}
				}
			});
		}
	}
}

