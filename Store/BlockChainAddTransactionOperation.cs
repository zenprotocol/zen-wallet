using System;
using System.Collections.Generic;
using BlockChain.Data;
using BlockChain.Database;
using BlockChain.Store;
using Consensus;
using Infrastructure;

namespace BlockChain
{
	public class AddedToMempoolMessage
	{
		public Keyed<Types.Transaction> Transaction { get; private set; }

		public AddedToMempoolMessage(Keyed<Types.Transaction> transaction)
		{
			Transaction = transaction;
		}
	}

	public class BlockChainAddTransactionOperation
	{
		private readonly TransactionContext _TransactionContext;
		private readonly Keyed<Types.Transaction> _NewTransaction; //TODO: or just use var key
		private readonly TxMempool _TxMempool;
		private readonly TxStore _TxStore;
		private readonly UTXOStore _UTXOStore;
		private Dictionary<Types.Outpoint, InputLocationEnum> _InputLocations;
		private MessageProducer<AddedToMempoolMessage> _Producer = MessageProducer<AddedToMempoolMessage>.Instance;

		private enum InputLocationEnum
		{
			Mempool,
			TxStore,
		}

		public enum Result
		{
			Added,
			AddedOrphaned,
			Rejected
		}

		public BlockChainAddTransactionOperation(TransactionContext transactionContext, Keyed<Types.Transaction> transaction, TxMempool txMempool)
		{
			_InputLocations = new Dictionary<Types.Outpoint, InputLocationEnum>();
			_TransactionContext = transactionContext;
			_NewTransaction = transaction;
			_TxMempool = txMempool;
			_TxStore = new TxStore();
			_UTXOStore = new UTXOStore();
		}

		//Ref: https://gitlab.com/mazudaoyi/zen-wallet/wikis/new-transaction-message
		public Result Start()
		{
			if (!IsValid() || IsInMempool() || IsInTxStore())
			{
				BlockChainTrace.Information("not valid/in mempool/in txstore");
				return Result.Rejected;
			}

			if (IsMempoolContainsSpendingInput())
			{
				BlockChainTrace.Information("Mempool contains spending input");
				return Result.Rejected;
			}

			if (IsOrphaned())
			{
				_TxMempool.Add(_NewTransaction, true);
				return Result.AddedOrphaned;
			}

			//TODO: 5. For each input, if the referenced transaction is coinbase, reject if it has fewer than COINBASE_MATURITY confirmations.

			if (!IsValidInputs())
			{
				return Result.Rejected;
			}

			//TODO: 7. Apply fee rules. If fails, reject
			//TODO: 8. Validate each input. If fails, reject

			_TxMempool.Add(_NewTransaction);

			//TODO: If the transaction is "ours", i.e. involves one of our addresses or contracts, tell the wallet.

			//Relay transaction to peers
			_Producer.PushMessage(new AddedToMempoolMessage(_NewTransaction));

			foreach (var transaction in _TxMempool.GetOrphanedsOf(_NewTransaction)) 
			{
				new BlockChainAddTransactionOperation(_TransactionContext, transaction, _TxMempool).Start();
			}

			return Result.Added;

		}

		private bool IsValid()
		{
			return true;
		}

		private bool IsInMempool()
		{
			return _TxMempool.ContainsKey(_NewTransaction.Key);
		}

		private bool IsInTxStore()
		{
			return _TxStore.ContainsKey(_TransactionContext, _NewTransaction.Key);
		}

		private bool IsMempoolContainsSpendingInput()
		{
			foreach (Types.Outpoint inputs in _NewTransaction.Value.inputs)
			{
				if (_TxMempool.ContainsInputs(_NewTransaction))
				{
					return true;
				}
			}

			return false;
		}

		private bool IsOrphaned()
		{
			foreach (Types.Outpoint input in _NewTransaction.Value.inputs)
			{
				if (_TxMempool.ContainsKey(input.txHash))
				{
					_InputLocations[input] = InputLocationEnum.Mempool;
				}
				else if (_TxStore.ContainsKey(_TransactionContext, input.txHash))
				{
					_InputLocations[input] = InputLocationEnum.TxStore;
				}
				else {
					return true;
				}
			}

			return false;
		}

		private bool IsValidInputs()
		{
			foreach (Types.Outpoint input in _NewTransaction.Value.inputs)
			{
				if (!ParentOutputExists(input))
				{
					return false;
				}

				if (ParentOutputSpent(input))
				{
					return false;
				}
			}

			return true;
		}

		private bool ParentOutputExists(Types.Outpoint input)
		{
			Keyed<Types.Transaction> parentTransaction = null;

			switch (_InputLocations[input])
			{
				case InputLocationEnum.Mempool:
					parentTransaction = _TxMempool.Get(input.txHash);
					break;
				case InputLocationEnum.TxStore:
					parentTransaction = _TxStore.Get(_TransactionContext, input.txHash);
					break;
			}

			if (input.index >= parentTransaction.Value.outputs.Length)
			{
				BlockChainTrace.Information("Input index not found");
				return false;
			}

			return true;
		}

		private bool ParentOutputSpent(Types.Outpoint input)
		{
			byte[] inputKey = Merkle.outpointHasher.Invoke(input);

			if (_InputLocations[input] == InputLocationEnum.TxStore && !_UTXOStore.ContainsKey(_TransactionContext, inputKey))
			{
				BlockChainTrace.Information("Output has been spent");
				return false;
			}

			return true;
		}
	}
}
