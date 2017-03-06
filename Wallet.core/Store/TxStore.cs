using System.Collections.Generic;
using BlockChain;
using BlockChain.Data;
using BlockChain.Store;
using Consensus;
using Store;

namespace Wallet.core
{
	//TODO: extend Types.Transaction to contain TxStateEnum, serialize+store using MsgPackStore

	public class TxBalancesStore : ConsensusTypeStore<Types.Transaction>
	{
		private static string BALANCES = "balances";
		private static string STATES = "states";
		public static string INDEXES = "indexes";

		public TxBalancesStore() : base("tx")
		{
		}

		public void Reset(TransactionContext dbTx)
		{
			dbTx.Transaction.RemoveAllKeys(_TableName, true);
			dbTx.Transaction.RemoveAllKeys(BALANCES, true);
			dbTx.Transaction.RemoveAllKeys(STATES, true);
			dbTx.Transaction.RemoveAllKeys(INDEXES, true);
		}

		public IEnumerable<Keyed<byte[], Types.Transaction>> All(TransactionContext dbTx)
		{
			foreach (var index in dbTx.Transaction.SelectForward<int, byte[]>(INDEXES))
			{
				yield return Get(dbTx, index.Value);
			}
		}

		public void Put(TransactionContext dbTx, byte[] txHash, Types.Transaction tx, AssetDeltas assetBalances, TxStateEnum txState)
		{
			//	dbTx.Transaction.SynchronizeTables(INDEXES);

			//if (!ContainsKey(dbTx, item.Key))
			//{
				int identity = 0;

				var row = dbTx.Transaction.Max<int, byte[]>(INDEXES);

				if (row.Exists)
					identity = row.Key;

				identity++;

				dbTx.Transaction.Insert<int, byte[]>(INDEXES, identity, txHash);
			//}

			Put(dbTx, txHash, tx);

			dbTx.Transaction.Insert<byte[], int>(STATES, txHash, (int)txState);

			SetBalances(dbTx, txHash, assetBalances);
		}

		public void SetBalances(TransactionContext dbTx, byte[] txHash, AssetDeltas assetBalances)
		{
			var table = dbTx.Transaction.InsertTable<byte[]>(BALANCES, txHash, 0);

			foreach (var asset in assetBalances)
			{
				table.Insert<byte[], long>(asset.Key, asset.Value);
			}
		}

		public AssetDeltas Balances(TransactionContext dbTx, byte[] tx)
		{
			var balances = new AssetDeltas();
			var table = dbTx.Transaction.SelectTable<byte[]>(BALANCES, tx, 0);

			foreach (var asset in table.SelectForward<byte[], long>())
			{
				balances[asset.Key] = asset.Value;
			}

			return balances;
		}

		public TxStateEnum TxState(TransactionContext dbTx, byte[] tx)
		{
			return (TxStateEnum) dbTx.Transaction.Select<byte[], int>(STATES, tx).Value;
		}

		public void SetTxState(TransactionContext dbTx, byte[] tx, TxStateEnum txState)
		{
			
			dbTx.Transaction.Insert<byte[], int>(STATES, tx, (int)txState);
		}

		//public bool Contains(TransactionContext dbTx, byte[] tx)
		//{
		//	return dbTx.Transaction.Select<byte[], int>(TXS, tx).Exists;
		//}
	}
}