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
			this.Where(t => t.TxHash.SequenceEqual(txDelta.TxHash)).ToList().ForEach(t=>Remove(t));

			base.Add(txDelta);
		}

        public AssetDeltas AssetDeltas {
            get
            {
                var assetDeltas = new AssetDeltas();

				ForEach(t => {
                    foreach (var item in t.AssetDeltas) {
				        if (!assetDeltas.ContainsKey(item.Key))
				            assetDeltas[item.Key] = 0;

				        assetDeltas[item.Key] += item.Value;
					}
				});

                return assetDeltas;
            }
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