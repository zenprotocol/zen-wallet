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
        public readonly AssetDeltas AssetDeltas = new AssetDeltas();

		public new void Add(TxDelta txDelta)
		{
			this.Where(t => t.TxHash.SequenceEqual(txDelta.TxHash)).ToList().ForEach(t=>Remove(t));

			AssetDeltas.Accumulate(txDelta.AssetDeltas);

			base.Add(txDelta);
		}

		public new void Remove(TxDelta txDelta)
		{
			AssetDeltas.Subtract(txDelta.AssetDeltas);

			base.Remove(txDelta);
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
		public void Accumulate(AssetDeltas assetDeltas)
		{
            AccumulateInner(assetDeltas);
		}

		public void Subtract(AssetDeltas assetDeltas)
		{
			AccumulateInner(assetDeltas, true);
		}

		public void AccumulateInner(AssetDeltas assetDeltas, bool isSubtract = false)
        {
            foreach (var item in assetDeltas)
            {
                if (!ContainsKey(item.Key))
                    this[item.Key] = 0;

                this[item.Key] += (isSubtract ? -1 : 1) * item.Value;
            }
        }
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