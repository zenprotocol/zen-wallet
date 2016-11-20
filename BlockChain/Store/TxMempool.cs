using System.Collections.Generic;
using BlockChain.Data;
using Consensus;
using System.Linq;
using Store;

namespace BlockChain.Store
{
	public class TxMempool
	{
		private readonly HashDictionary<Keyed<Types.Transaction>> _Transactions;
		private readonly List<Types.Outpoint> _TransactionsInputs;

		private readonly HashDictionary<Keyed<Types.Transaction>> _OrphanedTransactions;
		private readonly Dictionary<Types.Outpoint, Keyed<Types.Transaction>> _OrphanedTransactionsInputs;

		public TxMempool()
		{
			_Transactions = new HashDictionary<Keyed<Types.Transaction>>();
			_TransactionsInputs = new List<Types.Outpoint>();

			_OrphanedTransactions = new HashDictionary<Keyed<Types.Transaction>>();
			_OrphanedTransactionsInputs = new Dictionary<Types.Outpoint, Keyed<Types.Transaction>>();
		}

		public bool ContainsKey(byte[] key)
		{
			return _Transactions.ContainsKey(key);
		}

		public Keyed<Types.Transaction> Get(byte[] key)
		{
			return _Transactions[key];
		}

		public bool ContainsInputs(Keyed<Types.Transaction> transaction)
		{
			foreach (Types.Outpoint input in transaction.Value.inputs)
			{
				if (_TransactionsInputs.Contains(input)) {
					return true;
				}
			}

			return false;
	    }

		public IEnumerable<Keyed<Types.Transaction>> GetOrphanedsOf(Keyed<Types.Transaction> parentTransaction)
		{
			return _OrphanedTransactionsInputs.Keys
				.Where(key => key.txHash.SequenceEqual(parentTransaction.Key))
				.Select(key => _OrphanedTransactionsInputs[key]);
		}

		public void Add(Keyed<Types.Transaction> transaction, bool isOrphaned = false)
		{
			if (isOrphaned)
			{
				_OrphanedTransactions.Add(transaction.Key, transaction);

				foreach (Types.Outpoint input in transaction.Value.inputs)
				{
					_OrphanedTransactionsInputs.Add(input, transaction);
				}
			}
			else 
			{
				_Transactions.Add(transaction.Key, transaction);
				_TransactionsInputs.AddRange(transaction.Value.inputs);
			}
		}
	}
}