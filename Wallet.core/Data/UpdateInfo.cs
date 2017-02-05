using System;
using System.Collections.Generic;
using BlockChain.Data;
using Consensus;
using Store;

namespace Wallet.core
{
	public class ResetEventArgs
	{
		public TxDeltaItemsEventArgs TxDeltaList { get; set; }
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
		public AssetDeltas AssetDeltas { get; set; }

		public TxDelta(TxStateEnum txState, Types.Transaction transaction, AssetDeltas assetDeltas)
		{
			TxState = txState;
			Transaction = transaction;
			AssetDeltas = assetDeltas;
		}
	}
}
