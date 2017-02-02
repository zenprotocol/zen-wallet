using System;
using Wallet.core;
using Wallet.Domain;
using System.Linq;
using BlockChain.Data;
using System.Collections.Generic;
using Infrastructure;

namespace Wallet
{
	public class BalancesController : Singleton<BalancesController>
	{
		private ILogView _LogView;
		public ILogView LogView
		{
			get
			{
				return _LogView;
			}
			set
			{
				_LogView = value;
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

		public void AddNewBalances(WalletBalances walletBalances)
		{
			Gtk.Application.Invoke(delegate
			{
				//if (LogView != null)
				//{
				if (walletBalances.GetType() == typeof(ResetMessage))
				{
					LogView.Clear();
				}

				walletBalances.ForEach(u => u.Balances.ToList().ForEach(b => LogView.AddLogEntryItem(new LogEntryItem(
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

		//public void OnNewBalance(HashDictionary<long> balance)
		//{
		//	if (LogView != null)
		//	{
		//		foreach (var item in balance)
		//		{
		//			var asset = item.Key;

		//			var amount = item.Value;

		//			LogView.AddLogEntryItem(new LogEntryItem(
		//				(ulong)Math.Abs(amount),
		//				amount < 0 ? DirectionEnum.Sent : DirectionEnum.Recieved,
		//				AssetsHelper.Find(asset),
		//				DateTime.Now,
		//				Guid.NewGuid().ToString("N"),
		//				Guid.NewGuid().ToString("N"),
		//				0
		//			));
		//		}
		//	}
		//}

	}
}

