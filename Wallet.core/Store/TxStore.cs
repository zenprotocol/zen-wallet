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

	class TxStore : ConsensusTypeStore<ulong, TxData>
	{
		static string TX_HASHES_TO_IDENTITY = "wallet-tx-hashes";

		internal TxStore() : base("wallet-tx")
		{
		}

		internal void Reset(TransactionContext dbTx)
		{
			dbTx.Transaction.RemoveAllKeys(_TableName, true);
			dbTx.Transaction.RemoveAllKeys(TX_HASHES_TO_IDENTITY, true);
		}

		internal void Put(TransactionContext dbTx, byte[] txHash, Types.Transaction tx, AssetDeltas assetDeltas, TxStateEnum txState)
		{
			var txHashRecord = dbTx.Transaction.Select<byte[], ulong>(TX_HASHES_TO_IDENTITY, txHash);

			var identity = txHashRecord.Exists ? txHashRecord.Value : dbTx.Transaction.Count(TX_HASHES_TO_IDENTITY);

			if (!txHashRecord.Exists)
				dbTx.Transaction.Insert<byte[], ulong>(TX_HASHES_TO_IDENTITY, txHash, identity);

			Put(dbTx, identity, new TxData()
			{
				DateTime = DateTime.Now.ToUniversalTime(),
				TxState = txState,
				Tx = tx,
				AssetDeltas = assetDeltas,
				TxHash = txHash,
			});
		}
	}
}