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

	public class TxMempool
	{
		public readonly Pool Transactions = new Pool();
		private readonly Pool _ICTxs = new Pool();
		private readonly HashDictionary<Types.Transaction> _OrphanTransactions = new HashDictionary<Types.Transaction>();

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

		public bool Remove(byte[] key)
		{
			if (Transactions.ContainsKey(key))
			{
				Transactions.Remove(key);
				return true;
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

		public IEnumerable<Tuple<byte[], TransactionValidation.PointedTransaction>> GetDependenciesOfContract(byte[] contractHash)
		{
			//foreach (var item in _Transactions)
			//{
			//	foreach (var input in item.Value.pInputs)
			//	{
			//		if (!input.Item2.@lock.IsContractSacrificeLock || !((Types.OutputLock.IsContractSacrificeLock)input.Item2.@lock).contractHash.SequenceEqual(contractHash))
			//			continue;

			//		yield return new Tuple<byte[], TransactionValidation.PointedTransaction>(item.Key, item.Value);
			//	}
			//}
			return null;
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

		void MoveToICTxs(byte[] txHash)
		{
			lock (Transactions) //TODO: use a single HashDictionary with a 'location' enum?
			{
				var ptx = Transactions[txHash];

				Transactions.Remove(txHash);
				_ICTxs.Add(txHash, ptx);

				MoveToOrphansPool(txHash);
			}
		}

		public void InactivateContractGenerateTxs(byte[] contractHash)
		{
			var toInactivate = new List<byte[]>();
			foreach (var tx in Transactions)
			{
				byte[] txContractHash = null;
				if (BlockChain.IsContractGeneratedTx(tx.Value, out txContractHash) && contractHash.SequenceEqual(txContractHash))
					toInactivate.Add(tx.Key);
			}

			toInactivate.ForEach(MoveToICTxs);
		}

		void MoveToOrphansPool(byte[] txHash, Pool pool = null)
		{
			if (pool != null)
			{
				_OrphanTransactions.Add(txHash, TransactionValidation.unpoint(pool[txHash]));
				pool.Remove(txHash);
			}
			else
			{
				foreach (var tx in GetDependencies(txHash, Transactions))
				{
					MoveToOrphansPool(tx.Item1, Transactions);
				}

				foreach (var tx in GetDependencies(txHash, _ICTxs))
				{
					MoveToOrphansPool(tx.Item1, _ICTxs);
				}
			}
		}
	}
}