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

		public void SetTxView(ITransactionsView view)
		{
			Apply(view, App.Instance.Wallet.TxDeltaList);
			App.Instance.Wallet.OnReset += delegate { view.Clear(); };
			App.Instance.Wallet.OnItems += a => { Apply(view, a); };
		}

		public IWalletView WalletView { get; set; }

		private AssetType asset = App.Instance.Wallet.AssetsMetadata[Consensus.Tests.zhash];

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
					App.Instance.Wallet.AssetsMetadata[b.Key],
					u.Time,
					Guid.NewGuid().ToString("N"),
					BitConverter.ToString(u.TxHash),
					0,
					u.TxState
				))));
			});
		}
	}
}

