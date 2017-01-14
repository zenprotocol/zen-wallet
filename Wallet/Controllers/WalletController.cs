using System;
using System.Threading;
using Wallet.Domain;
using Infrastructure;
using System.Linq;
using Wallet.core;
using NBitcoinDerive;
using Consensus;

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

		//public CurrencyEnum currency;
		//public String currencyStr;

		private AssetType asset = AssetsManager.AssetTypes["zen"];

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
			foreach (var transactionSpendData in App.Instance.Wallet.MyTransactions)
			{
				HandleNewTransaction(transactionSpendData);
			}

			App.Instance.Wallet.OnNewTransaction += HandleNewTransaction;
		}

		public void Sync()
		{
			TransactionsView.Clear();

			foreach (var transactionSpendData in App.Instance.Wallet.MyTransactions)
			{
				HandleNewTransaction(transactionSpendData);
			}
		}

		public void HandleNewTransaction(TransactionSpendData transactionSpendData)
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
					foreach (var item in transactionSpendData.Balances)
					{
						var asset = item.Key;
						var amount = item.Value;

						TransactionsView.AddTransactionItem(new TransactionItem(
							amount < 0 ? -1 * amount : amount,
							amount < 0 ? DirectionEnum.Sent : DirectionEnum.Recieved,
							AssetsManager.Find(asset),
							DateTime.Now,
							Guid.NewGuid().ToString("N"),
							Guid.NewGuid().ToString("N"),
							amount
						));
					}
				}
			});
		}
	}
}

