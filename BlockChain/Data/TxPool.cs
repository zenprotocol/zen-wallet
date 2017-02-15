using System.Collections.Generic;
using BlockChain.Data;
using Consensus;
using System.Linq;
using Store;
using Infrastructure;
using System;

namespace BlockChain.Data
{
	public class Pool : HashDictionary<TransactionValidation.PointedTransaction>
	{
	}

	public class TxPool
	{
		public readonly Pool Transactions = new Pool();
		public readonly Pool ICTxs = new Pool();
		private readonly HashDictionary<Types.Transaction> _OrphanTransactions = new HashDictionary<Types.Transaction>();
		public readonly ContractPool ContractPool = new ContractPool();

		private object _Lock = new Object();

		public void Lock(Action action)
		{
			lock (_Lock)
			{
				action();
			}
		}

		public bool ContainsKey(byte[] key)
		{
			return Transactions.ContainsKey(key) || _OrphanTransactions.ContainsKey(key);
		}

		public bool Remove(byte[] key, List<byte[]> removedList = null)
		{
			if (Transactions.ContainsKey(key))
			{
				foreach (var dep in GetDependencies(key))
				{
					if (!Remove(dep.Item1))
						throw new Exception();
				}

				if (removedList != null)
				{
					removedList.Add(key);
				}

				return Transactions.Remove(key);
			}
			else
			{
				return false;
				//_OrphanTransactions.Remove(key);
			}
		}

		public void RemoveOrphan(byte[] key)
		{
			_OrphanTransactions.Remove(key);
		}

		public TransactionValidation.PointedTransaction Get(byte[] key)
		{
			return Transactions[key];
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
			foreach (var item in Transactions)
			{
				if (item.Value.pInputs.Select(t => t.Item1).Contains(outpoint))
				{
					return true;
				}
			}

			return false;
		}

		public IEnumerable<Tuple<byte[], TransactionValidation.PointedTransaction>> GetTransactionsInConflict(Types.Transaction tx)
		{
			foreach (var item in Transactions)
			{
				foreach (var memOutpoint in item.Value.pInputs.Select(t => t.Item1))
				{
					foreach (var txOutpoint in tx.inputs)
					{
						if (memOutpoint.Equals(txOutpoint))
						{
							yield return new Tuple<byte[], TransactionValidation.PointedTransaction>(item.Key, item.Value);
						}
					}
				}
			}
		}

		public IEnumerable<Tuple<byte[], TransactionValidation.PointedTransaction>> GetDependencies(byte[] txHash)
		{
			return GetDependencies(txHash, Transactions);
		}

		public IEnumerable<Tuple<byte[], TransactionValidation.PointedTransaction>> GetDependencies(byte[] txHash, Pool pool)
		{
			foreach (var item in pool)
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
			Transactions.Add(key, transaction);
		}

		public void AddOrphan(byte[] txHash, Types.Transaction tx)
		{
			_OrphanTransactions.Add(txHash, tx);
		}

		void MoveToICTxs(TransactionContext dbTx, byte[] txHash)
		{
			lock (Transactions) //TODO: use a single HashDictionary with a 'location' enum?
			{
				var ptx = Transactions[txHash];

				Transactions.Remove(txHash);
				ICTxs.Add(txHash, ptx);

				MoveToOrphanPool(dbTx, txHash);
			}
		}

		public void InactivateContractGeneratedTxs(TransactionContext dbTx, byte[] contractHash)
		{
			var toInactivate = new List<byte[]>();
			foreach (var tx in Transactions)
			{
				byte[] txContractHash = null;
				if (BlockChain.IsContractGeneratedTx(tx.Value, out txContractHash) && contractHash.SequenceEqual(txContractHash))
					toInactivate.Add(tx.Key);
			}

			toInactivate.ForEach(t => MoveToICTxs(dbTx, t));
		}

		void MoveToOrphanPool(TransactionContext dbTx, byte[] txHash, Pool pool = null)
		{
			if (pool != null)
			{
				_OrphanTransactions.Add(txHash, TransactionValidation.unpoint(pool[txHash]));
				pool.Remove(txHash);

				if (pool == Transactions)
				{
					ContractPool.RemoveRef(txHash, dbTx, this);
				}
			}
			else
			{
				foreach (var tx in GetDependencies(txHash, Transactions))
				{
					MoveToOrphanPool(dbTx, tx.Item1, Transactions);
				}

				foreach (var tx in GetDependencies(txHash, ICTxs))
				{
					MoveToOrphanPool(dbTx, tx.Item1, ICTxs);
				}
			}
		}
	}
}