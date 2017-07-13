using System;
using Store;
using System.Linq;

namespace BlockChain.Store
{
	public class ContractsTxsStore
	{
		private const string TABLE = "contracts-txs";

		public static void Add(DBreeze.Transactions.Transaction dbTx, byte[] contactHash, byte[] txHash)
		{
			dbTx.Insert<byte[], byte[]>(TABLE, contactHash, txHash);
		}

		public static byte[] Get(DBreeze.Transactions.Transaction dbTx, byte[] contactHash)
		{
			var row = dbTx.Select<byte[], byte[]>(TABLE, contactHash);

			return row.Exists ? row.Value : null;
		}
	}
}
