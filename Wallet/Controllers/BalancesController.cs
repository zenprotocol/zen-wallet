using System;
using Wallet.core;
using Wallet.Domain;
using System.Linq;
using BlockChain.Data;
using System.Collections.Generic;

namespace Wallet
{
	public class BalancesController
	{
		private static BalancesController instance = null;
		public ILogView LogView { get; set; }

		public static BalancesController GetInstance() {
			if (instance == null) {
				instance = new BalancesController ();
			}

			return instance;
		}
			
		public BalancesController ()
		{
			App.Instance.Wallet.OnNewBalance += OnNewBalance;
		}

		public void Sync()
		{
			LogView.Clear();
			AddNewBalances(App.Instance.Wallet.Load());
		}

		public void AddNewBalances(HashDictionary<List<long>> balances)
		{
			if (LogView != null)
			{
				foreach (var item in balances)
				{
					var asset = item.Key;

					foreach (var item_ in item.Value)
					{
						var amount = item.Value;

						LogView.AddLogEntryItem(new LogEntryItem(
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
		}

		public void OnNewBalance(HashDictionary<long> balance)
		{
			if (LogView != null)
			{
				foreach (var item in balance)
				{
					var asset = item.Key;

					var amount = item.Value;

					LogView.AddLogEntryItem(new LogEntryItem(
						(ulong)Math.Abs(amount),
						amount < 0 ? DirectionEnum.Sent : DirectionEnum.Recieved,
						AssetsHelper.Find(asset),
						DateTime.Now,
						Guid.NewGuid().ToString("N"),
						Guid.NewGuid().ToString("N"),
						0
					));
				}
			}
		}

	}
}

