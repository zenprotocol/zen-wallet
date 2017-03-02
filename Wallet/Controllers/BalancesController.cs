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
		public void SetLogView(ILogView logView)
		{
			Apply(logView, App.Instance.Wallet.TxDeltaList);
			App.Instance.Wallet.OnReset += delegate { logView.Clear(); };
			App.Instance.Wallet.OnItems += a => { Apply(logView, a); };
		}

		public void Apply(ILogView view, TxDeltaItemsEventArgs deltas)
		{
			Gtk.Application.Invoke(delegate
			{
				deltas.ForEach(u => u.AssetDeltas.ToList().ForEach(b => view.AddLogEntryItem(new LogEntryItem(
					Math.Abs(b.Value),
					b.Value < 0 ? DirectionEnum.Sent : DirectionEnum.Recieved,
					App.Instance.Wallet.AssetsMetadata[b.Key],
					DateTime.Now,
					Guid.NewGuid().ToString("N"),
					Guid.NewGuid().ToString("N"),
					0
				))));
			});
		}
	}
}

