using System;
using BlockChain.Data;
using BlockChain.Store;
using Consensus;
using Store;

namespace Wallet.core
{
	public class TxData
	{
		public byte[] TxHash { get; set; }
		public Types.Transaction Tx { get; set; }
		public TxStateEnum TxState { get; set; }
		public DateTime DateTime { get; set; }
		public AssetDeltas AssetDeltas { get; set; }
	}

	class TxStore : ConsensusTypeStore<int, TxData>
	{
		static string TX_HASHES_TO_IDENTITY = "tx-hashes";

		internal TxStore() : base("tx")
		{
		}

		internal void Reset(TransactionContext dbTx)
		{
			dbTx.Transaction.RemoveAllKeys(_TableName, true);
			dbTx.Transaction.RemoveAllKeys(TX_HASHES_TO_IDENTITY, true);
		}

		internal void Put(TransactionContext dbTx, byte[] txHash, Types.Transaction tx, AssetDeltas assetDeltas, TxStateEnum txState)
		{
			var txHashRecord = dbTx.Transaction.Select<byte[], int>(TX_HASHES_TO_IDENTITY, txHash);
			Put(dbTx, txHashRecord.Exists ? txHashRecord.Value : (int)dbTx.Transaction.Count(_TableName), new TxData()
			{
				DateTime = DateTime.Now,
				TxState = txState,
				Tx = tx,
				AssetDeltas = assetDeltas,
				TxHash = txHash,
			});
		}
	}
}