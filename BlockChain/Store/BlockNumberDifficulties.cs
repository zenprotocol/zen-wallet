using System;
using Store;
using System.Linq;

namespace BlockChain.Store
{
	public class BlockNumberDifficulties
	{
		private const string TABLE = "bk-number-difficulty";

		public void Add(DBreeze.Transactions.Transaction dbTx, uint blockNumber, byte[] block)
		{
			dbTx.Insert<uint, byte[]>(TABLE, blockNumber, block);
		}

		public byte[] GetLast(DBreeze.Transactions.Transaction dbTx)
		{
			var record = dbTx.SelectBackward<uint, byte[]>(TABLE).First();
			return record == null ? null : record.Value;
		}
	}
}
