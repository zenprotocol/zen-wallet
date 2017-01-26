using System.Collections.Generic;
using BlockChain.Data;
using Consensus;
using System.Linq;
using Store;
using Infrastructure;
using System;

namespace BlockChain.Store
{
	public class TxMempool
	{
		private readonly HashDictionary<Keyed<Types.Transaction>> _Transactions;
		private readonly List<Types.Outpoint> _TransactionsInputs;

		private readonly HashDictionary<Keyed<Types.Transaction>> _OrphanTransactions;
		private readonly Dictionary<Types.Outpoint, Keyed<Types.Transaction>> _OrphanTransactionsInputs;

		private object _Lock = new Object();

		public void Lock(Action action)
		{
			lock (_Lock)
			{
				action();
			}
		}

		//demo
		public List<Keyed<Types.Transaction>> GetAll()
		{
			lock (_Lock)
			{
				var result = new List<Keyed<Types.Transaction>>();

				foreach (var item in _Transactions)
				{
					result.Add(item.Value);
				}

				return result;
			}
		}

		public TxMempool()
		{
			_Transactions = new HashDictionary<Keyed<Types.Transaction>>();
			_TransactionsInputs = new List<Types.Outpoint>();

			_OrphanTransactions = new HashDictionary<Keyed<Types.Transaction>>();
			_OrphanTransactionsInputs = new Dictionary<Types.Outpoint, Keyed<Types.Transaction>>();
		}

		public bool ContainsKey(byte[] key)
		{
			lock (_Lock)
			{
				return _Transactions.ContainsKey(key) || _OrphanTransactions.ContainsKey(key);
			}
		}

		public void Remove(byte[] key)
		{
			lock (_Lock)
			{
				if (_Transactions.ContainsKey(key))
					_Transactions.Remove(key);
				else
					_OrphanTransactions.Remove(key);
			}
		}

		public Keyed<Types.Transaction> Get(byte[] key)
		{
			lock (_Lock)
			{
				return _Transactions[key];
			}
		}

		public bool ContainsInputs(Keyed<Types.Transaction> transaction)
		{
			lock (_Lock)
			{
				foreach (Types.Outpoint input in transaction.Value.inputs)
				{
					if (_TransactionsInputs.Contains(input))
					{
						return true;
					}
				}

				return false;
			}
		}

		public IEnumerable<Keyed<Types.Transaction>> GetOrphansOf(Keyed<Types.Transaction> parentTransaction)
		{
			lock (_Lock)
			{
				return _OrphanTransactionsInputs.Keys
					.Where(key => key.txHash.SequenceEqual(parentTransaction.Key))
					.Select(key => _OrphanTransactionsInputs[key]);
			}
		}

		public void Add(Keyed<Types.Transaction> transaction, bool isOrphan = false)
		{
			lock (_Lock)
			{
				if (isOrphan)
				{
					_OrphanTransactions.Add(transaction.Key, transaction);

					foreach (Types.Outpoint input in transaction.Value.inputs)
					{
						_OrphanTransactionsInputs.Add(input, transaction);
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
}