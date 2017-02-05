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

		public void SetTxView(ITransactionsView view)
		{
			Apply(view, App.Instance.Wallet.TxDeltaList);
			App.Instance.Wallet.OnReset += delegate { view.Clear(); };
			App.Instance.Wallet.OnItems += a => { Apply(view, a); };
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

		public void Apply(ITransactionsView view, TxDeltaItemsEventArgs deltas)
		{
			//if (ActionBarView != null)
			//{
			//	//Alternative: Runtime.DispatchService.GuiDispatch (new StatefulMessageHandler (UpdateGui), n);
			//	ActionBarView.Total = (decimal)10000;
			//	ActionBarView.Rate = (decimal)10000;
			//}
			Gtk.Application.Invoke(delegate
			{
				deltas.ForEach(u => u.AssetDeltas.ToList().ForEach(b => view.AddTransactionItem(new TransactionItem(
					Math.Abs(b.Value),
					b.Value < 0 ? DirectionEnum.Sent : DirectionEnum.Recieved,
					AssetsHelper.Find(b.Key),
					DateTime.Now,
					Guid.NewGuid().ToString("N"),
					Guid.NewGuid().ToString("N"),
					0
				))));
			});
		}
	}
}

