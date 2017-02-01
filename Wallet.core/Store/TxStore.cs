using System.Collections.Generic;
using BlockChain.Data;
using BlockChain.Store;
using Consensus;
using Store;

namespace Wallet.core
{
	public enum TxStateEnum
	{
		Unconfirmed = 1,
		Confirmed = 2,
		Invalid = 3,
	}

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

		public IEnumerable<Keyed<Types.Transaction>> All(TransactionContext dbTx)
		{
			foreach (var index in dbTx.Transaction.SelectForward<int, byte[]>(INDEXES))
			{
				yield return Get(dbTx, index.Value);
			}
		}

		public void Put(TransactionContext dbTx, Keyed<Types.Transaction> item, HashDictionary<long> assetBalances, TxStateEnum txState)
		{
		//	dbTx.Transaction.SynchronizeTables(INDEXES);

			int identity = 0;

			var row = dbTx.Transaction.Max<int, byte[]>(INDEXES);

			if (row.Exists)
				identity = row.Key;

			identity++;

			dbTx.Transaction.Insert<int, byte[]>(INDEXES, identity, item.Key);

			Put(dbTx, item);
			dbTx.Transaction.Insert<byte[], int>(STATES, item.Key, (int)txState);

			var table = dbTx.Transaction.InsertTable<byte[]>(BALANCES, item.Key, 0);

			foreach (var asset in assetBalances)
			{
				table.Insert<byte[], long>(asset.Key, asset.Value);
			}
		}

		public HashDictionary<long> Balances(TransactionContext dbTx, byte[] tx)
		{
			var balances = new HashDictionary<long>();
			var table = dbTx.Transaction.SelectTable<byte[]>(BALANCES, tx, 0);

			foreach (var asset in table.SelectForward<byte[], long>())
			{
				balances[asset.Key] = asset.Value;
			}

			return balances;
		}

		//public bool Contains(TransactionContext dbTx, byte[] tx)
		//{
		//	return dbTx.Transaction.Select<byte[], int>(TXS, tx).Exists;
		//}
	}
}