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
		private readonly HashDictionary<TransactionValidation.PointedTransaction> _Transactions;
		private readonly List<Types.Outpoint> _TransactionOutpoints;

		private readonly HashDictionary<Keyed<Types.Transaction>> _OrphanTransactions;
		private readonly Dictionary<Types.Outpoint, Keyed<Types.Transaction>> _OrphanTransactionsOutpoints;

		private object _Lock = new Object();

		public void Lock(Action action)
		{
			lock (_Lock)
			{
				action();
			}
		}

		//demo
		public List<TransactionValidation.PointedTransaction> GetAll()
		{
			lock (_Lock)
			{
				var result = new List<TransactionValidation.PointedTransaction>();

				foreach (var item in _Transactions)
				{
					result.Add(item.Value);
				}

				return result;
			}
		}

		public TxMempool()
		{
			_Transactions = new HashDictionary<TransactionValidation.PointedTransaction>();
			_TransactionOutpoints = new List<Types.Outpoint>();

			_OrphanTransactions = new HashDictionary<Keyed<Types.Transaction>>();
			_OrphanTransactionsOutpoints = new Dictionary<Types.Outpoint, Keyed<Types.Transaction>>();
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

		public TransactionValidation.PointedTransaction Get(byte[] key)
		{
			lock (_Lock)
			{
				return _Transactions[key];
			}
		}

		public bool ContainsInputs(Types.Transaction tx)
		{
			lock (_Lock)
			{
				foreach (var outpoint in tx.inputs)
				{
					if (ContainsOutpoint(outpoint))
					{
						return true;
					}
				}

				return false;
			}
		}

		public bool ContainsOutpoint(Types.Outpoint outpoint)
		{
			return _TransactionOutpoints.Contains(outpoint);
		}

		public IEnumerable<Tuple<byte[], TransactionValidation.PointedTransaction>> GetTransactionsInConflict(Types.Transaction tx)
		{
			lock (_Lock)
			{
				foreach (var item in _Transactions)
				{
					foreach (var _outpoint in item.Value.pInputs.Select(t=>t.Item1))
					{
						foreach (var outpoint in tx.inputs)
						{
							if (outpoint.Equals(_outpoint))
							{
								yield return new Tuple<byte[], TransactionValidation.PointedTransaction>(item.Key, item.Value);
							}
						}
					}
				}
			}
		}

		public IEnumerable<Keyed<Types.Transaction>> GetOrphansOf(Keyed<Types.Transaction> parentTransaction)
		{
			lock (_Lock)
			{
				return _OrphanTransactionsOutpoints.Keys
					.Where(key => key.txHash.SequenceEqual(parentTransaction.Key))
					.Select(key => _OrphanTransactionsOutpoints[key]);
			}
		}

		public void Add(byte[] key, TransactionValidation.PointedTransaction transaction)
		{
			lock (_Lock)
			{
				_Transactions.Add(key, transaction);
				_TransactionOutpoints.AddRange(transaction.pInputs.Select(t=>t.Item1));
			}
		}

		public void AddOrphan(Keyed<Types.Transaction> transaction)
		{
			lock (_Lock)
			{
				_OrphanTransactions.Add(transaction.Key, transaction);

				foreach (Types.Outpoint input in transaction.Value.inputs)
				{
					_OrphanTransactionsOutpoints.Add(input, transaction);
				}
			}
		}

}
}