using System.Collections.Generic;
using BlockChain.Data;
using Consensus;
using System.Linq;
using Store;
using Infrastructure;
using System;

namespace BlockChain.Data
{
	public class TxMempool
	{
		private readonly HashDictionary<TransactionValidation.PointedTransaction> _Transactions;
		//private readonly List<Types.Outpoint> _TransactionOutpoints;

		private readonly HashDictionary<Types.Transaction> _OrphanTransactions;
		//private readonly Dictionary<Types.Outpoint, Keyed<Types.Transaction>> _OrphanTransactionsOutpoints;

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
			var result = new List<TransactionValidation.PointedTransaction>();

			foreach (var item in _Transactions)
			{
				result.Add(item.Value);
			}

			return result;
		}

		public TxMempool()
		{
			_Transactions = new HashDictionary<TransactionValidation.PointedTransaction>();
			//_TransactionOutpoints = new List<Types.Outpoint>();

			_OrphanTransactions = new HashDictionary<Types.Transaction>();
		//	_OrphanTransactionsOutpoints = new Dictionary<Types.Outpoint, Keyed<Types.Transaction>>();
		}

		public bool ContainsKey(byte[] key)
		{
			return _Transactions.ContainsKey(key) || _OrphanTransactions.ContainsKey(key);
		}

		public bool Remove(byte[] key)
		{
			if (_Transactions.ContainsKey(key))
			{
				_Transactions.Remove(key);
				return true;
			}
			else
			{
				return false;
				_OrphanTransactions.Remove(key);
			}
		}

		public void RemoveOrphan(byte[] key)
		{
			_OrphanTransactions.Remove(key);
		}

		public TransactionValidation.PointedTransaction Get(byte[] key)
		{
			return _Transactions[key];
		}

		public bool ContainsInputs(Types.Transaction tx)
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

		public bool ContainsOutpoint(Types.Outpoint outpoint)
		{
			foreach (var item in _Transactions)
			{
				if (item.Value.pInputs.Select(t => t.Item1).Contains(outpoint))
				{
					return true;
				}
			}

			return false;
					
			//return _TransactionOutpoints.Contains(outpoint);
		}

		//internal void ValidateOrphansOf(byte[] key)
		//{
		//	foreach (var item in GetOrphansOf(key))
		//	{

		//	}
		//}

		public IEnumerable<Tuple<byte[], TransactionValidation.PointedTransaction>> GetTransactionsInConflict(Types.Transaction tx)
		{
			foreach (var item in _Transactions)
			{
				foreach (var _outpoint in item.Value.pInputs.Select(t => t.Item1))
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

		public IEnumerable<Tuple<byte[], TransactionValidation.PointedTransaction>> GetDependencies(byte[] txHash)
		{
			foreach (var item in _Transactions)
			{
				if (item.Value.pInputs.Count(t => t.Item1.txHash.SequenceEqual(txHash)) > 0)
				{
					yield return new Tuple<byte[], TransactionValidation.PointedTransaction>(item.Key, item.Value);
				}
			}
		}

		public IEnumerable<Tuple<byte[], Types.Transaction>> GetOrphansOf(byte[] txHash)
		{
			foreach (var item in _OrphanTransactions)
			{
				if (item.Value.inputs.Count(t => t.txHash.SequenceEqual(txHash)) > 0)
				{
					yield return new Tuple<byte[], Types.Transaction>(item.Key, item.Value);
				}
			}
		}

		public void Add(byte[] key, TransactionValidation.PointedTransaction transaction)
		{
			_Transactions.Add(key, transaction);
		//	_TransactionOutpoints.AddRange(transaction.pInputs.Select(t => t.Item1));
		}

		public void AddOrphan(byte[] txHash, Types.Transaction tx)
		{
			_OrphanTransactions.Add(txHash, tx);

			//foreach (Types.Outpoint input in transaction.Value.inputs)
			//{
			//	_OrphanTransactionsOutpoints.Add(input, transaction);
			//}
		}
	}
}