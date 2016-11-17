using System.Collections.Generic;
using BlockChain.Data;
using Consensus;

namespace BlockChain.Store
{
	public class TxMempoolItem
	{
		public Types.Transaction Transaction { get; set; }
		public bool IsOrphaned { get; set; }
	}

	public class TxMempool
	{
		private readonly HashDictionary<TxMempoolItem> _Transactions;
		private readonly List<Types.Outpoint> _Outpoints;

		public TxMempool()
		{
			_Transactions = new HashDictionary<TxMempoolItem>();
			_Outpoints = new List<Types.Outpoint>();
		}

		public bool ContainsKey(byte[] key)
		{
			return _Transactions.ContainsKey(key);
		}

		public Types.Transaction Get(byte[] key)
		{
			return _Transactions[key].Transaction;
		}

		public bool ContainsInputs(Types.Transaction transaction)
		{
			foreach (Types.Outpoint outpoint in transaction.inputs)
			{
				if (_Outpoints.Contains(outpoint)) {
					return true;
				}
			}

			return false;
	    }

		public void AddOrphaned(Keyed<Types.Transaction> transaction)
		{
			_Add(transaction, true);
		}

		public void Add(Keyed<Types.Transaction> transaction)
		{
			_Add(transaction);
		}

		private void _Add(Keyed<Types.Transaction> transaction, bool isOrphaned = false)
		{
			TxMempoolItem txMempoolItem = new TxMempoolItem() { Transaction = transaction.Value, IsOrphaned = isOrphaned };

			_Transactions.Add(transaction.Key, txMempoolItem);
			_Outpoints.AddRange(transaction.Value.inputs);
		}
	}
}
