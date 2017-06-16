using System;
using System.Collections.Generic;
using BlockChain.Data;
using Consensus;
using System.Linq;

namespace Wallet.core
{
	public class ResetEventArgs
	{
		public TxDeltaItemsEventArgs TxDeltaList { get; set; }
	}

	public class AggregatingTxDeltaItemsEventArgs : TxDeltaItemsEventArgs
	{
		public new void Add(TxDelta txDelta)
		{
			if (txDelta.TxState != TxStateEnum.Confirmed)
			{
				var removing = new List<TxDelta>();

				ForEach(t =>
				{
					if (t.TxHash.SequenceEqual(txDelta.TxHash))
						removing.Add(t);
				});

				removing.ForEach(t => Remove(t));
			}

			base.Add(txDelta);
		}

		public new void AddRange(IEnumerable<TxDelta> items)
		{
			foreach (var item in items)
				Add(item);
		}
	}

	public class TxDeltaItemsEventArgs : List<TxDelta>
	{

	}

	public class AssetDeltas : HashDictionary<long>
	{
	}

	public class TxDelta
	{
		public TxStateEnum TxState { get; set; }
		public Types.Transaction Transaction { get; set; }
		public byte[] TxHash { get; set; }
		public AssetDeltas AssetDeltas { get; set; }
		public DateTime Time { get; set; }

		public TxDelta(TxStateEnum txState, byte[] txHash, Types.Transaction transaction, AssetDeltas assetDeltas) : this(txState, txHash, transaction, assetDeltas, DateTime.Now.ToUniversalTime())
		{
		}

		public TxDelta(TxStateEnum txState, byte[] txHash, Types.Transaction transaction, AssetDeltas assetDeltas, DateTime time)
		{
			TxState = txState;
			TxHash = txHash;
			Transaction = transaction;
			AssetDeltas = assetDeltas;
			Time = time;
		}
	}
}