using System;
using BlockChain.Data;
using BlockChain.Database;
using BlockChain.Store;
using Consensus;

namespace BlockChain
{
	public class BlockChainAddTransactionOperation
	{
		private readonly TransactionContext _TransactionContext;
		private readonly Keyed<Types.Transaction> _KeyedTransaction; //TODO: or just use var key
		private readonly TxMempool _TxMempool;
		private readonly TxStore _TxStore;

		public enum Result
		{
			Added,
			AddedOrphanded,
			Rejected
		}

		public BlockChainAddTransactionOperation(TransactionContext transactionContext, Types.Transaction transaction, TxMempool txMempool, TxStore txStore)
		{
			_TransactionContext = transactionContext;
			_KeyedTransaction = new Keyed<Types.Transaction>(transaction, Merkle.transactionHasher.Invoke(transaction));
			_TxMempool = txMempool;
			_TxStore = txStore;
		}

		public Result Start()
		{
			if (!IsValid() || IsInMempool() || IsInTxStore())
			{
				return Result.Rejected;
			}

			if (IsOrphaned())
			{
				_TxMempool.AddOrphaned(_KeyedTransaction);
				return Result.AddedOrphanded;
			}

			//TODO: 5. For each input, if the referenced transaction is coinbase, reject if it has fewer than COINBASE_MATURITY confirmations.

			if (!OutputsExist())
			{
				return Result.Rejected;
			}

			//TODO: 7. Apply fee rules. If fails, reject
			//TODO: 8. Validate each input. If fails, reject

			_TxMempool.Add(_KeyedTransaction);
			return Result.Added;

			//TODO:
			/*
				If the transaction is "ours", i.e. involves one of our addresses or contracts, tell the wallet.
				Relay transaction to peers.
				For each orphan transaction in the mempool that spends an output of NEWTX, apply this transaction validation procedure to that orphan.
			*/
		}

		private bool IsValid()
		{
			return true;
		}

		private bool IsInMempool()
		{
			return _TxMempool.ContainsKey(_KeyedTransaction.Key);
		}

		private bool IsInTxStore()
		{
			return _TxStore.ContainsKey(_TransactionContext, _KeyedTransaction.Key);
		}

		private bool IsOrphaned()
		{
			foreach (Types.Outpoint inputs in _KeyedTransaction.Value.inputs)
			{
				if (!_TxStore.ContainsKey(_TransactionContext, inputs.txHash))
				{
					return true;
				}

				if (!_TxMempool.ContainsKey(inputs.txHash))
				{
					return true;
				}
			}

			return false;
		}

		private bool OutputsExist()
		{
			foreach (Types.Outpoint inputs in _KeyedTransaction.Value.inputs)
			{
				if (!TxOutputExists(inputs))
				{
					return false;
				}
			}

			return true;
		}

		private bool TxOutputExists(Types.Outpoint inputs)
		{
			throw new Exception();
		}
	}
}
