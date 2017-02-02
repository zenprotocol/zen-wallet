using System;
using Wallet.Domain;
using Wallet.core;
using BlockChain.Data;
using Infrastructure;
using System.Linq;

namespace Wallet
{
	public class WalletController : Singleton<WalletController>
	{
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

		private TransactionsView _TransactionsView;
		public TransactionsView TransactionsView { 
			get 
			{
				return _TransactionsView;
			}
			set 
			{
				_TransactionsView = value;
				AddNewBalances(App.Instance.Wallet.WalletBalances);

				MessageProducer<IWalletMessage>.Instance.AddMessageListener(new MessageListener<IWalletMessage>(m =>
				{
				//	if (m.GetType() == typeof(WalletBalances))
				//	{
					AddNewBalances(m as WalletBalances);
				//	}
				}));
			} 
		}

		public IWalletView WalletView { get; set; }

		private AssetType asset = AssetsHelper.AssetTypes["zen"];

		public AssetType Asset { 
			set {
				asset = value;
				ActionBarView.Asset = value;
				WalletView.ActionBar = !(value is AssetTypeAll);
			} get {
				return asset;
			}
		}

		public void AddNewBalances(WalletBalances walletBalances)
		{
			//if (ActionBarView != null)
			//{
			//	//Alternative: Runtime.DispatchService.GuiDispatch (new StatefulMessageHandler (UpdateGui), n);
			//	ActionBarView.Total = (decimal)10000;
			//	ActionBarView.Rate = (decimal)10000;
			//}
			Gtk.Application.Invoke(delegate
			{
				//if (TransactionsView != null)
				//{

				if (walletBalances.GetType() == typeof(ResetMessage))
				{
					TransactionsView.Clear();
				}

				walletBalances.ForEach(u => u.Balances.ToList().ForEach(b => TransactionsView.AddTransactionItem(new TransactionItem(
					Math.Abs(b.Value),
					b.Value < 0 ? DirectionEnum.Sent : DirectionEnum.Recieved,
					AssetsHelper.Find(b.Key),
					DateTime.Now,
					Guid.NewGuid().ToString("N"),
					Guid.NewGuid().ToString("N"),
					0
				))));
				//}
			});
		}
	}
}

