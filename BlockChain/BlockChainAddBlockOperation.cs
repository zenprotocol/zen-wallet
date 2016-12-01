using System;
using System.Collections.Generic;
using Store;
using BlockChain.Store;
using Consensus;
using Infrastructure;

namespace BlockChain
{
	public class BlockChainAddBlockOperation
	{
		private readonly TransactionContext _TransactionContext;
		private readonly BlockStore _MainBlockStore;
		private readonly BlockStore _BranchBlockStore;
		private readonly BlockStore _OrphanBlockStore;
		private readonly Keyed<Types.Block> _NewBlock; //TODO: or just use var key

		public enum Result
		{
			Added,
			AddedOrphaned,
			Rejected
		}

		public BlockChainAddBlockOperation(
			TransactionContext transactionContext,
       		Keyed<Types.Block> block,
			BlockStore _MainBlockStore, 
			BlockStore _BranchBlockStore, 
			BlockStore _OrphanBlockStore
		)
		{
		}

		public Result Start()
		{
			if (!IsValid() || IsInStore())
			{
				BlockChainTrace.Information("not valid/in store");
				return Result.Rejected;
			}

			if (!IsValidInputs())
			{
				BlockChainTrace.Information("invalid inputs");
				return Result.Rejected;
			}

			//TODO: 7. Apply fee rules. If fails, reject
			//TODO: 8. Validate each input. If fails, reject

			//_TxMempool.Add(_NewTransaction);

			//foreach (var transaction in _TxMempool.GetOrphanedsOf(_NewTransaction)) 
			//{
			//	BlockChainTrace.Information("Start with orphan");
			//	new BlockChainAddTransactionOperation(_TransactionContext, transaction, _TxMempool).Start();
			//}

			return Result.Added;
		}

		private bool IsValid()
		{
			return true;
		}

		private bool IsInStore()
		{
			return
				_MainBlockStore.ContainsKey(_TransactionContext, _NewBlock.Key) ||
				_BranchBlockStore.ContainsKey(_TransactionContext, _NewBlock.Key) ||
			  	_OrphanBlockStore.ContainsKey(_TransactionContext, _NewBlock.Key);
		}

		private bool IsValidInputs()
		{
			foreach (Types.Transaction transaction in _NewBlock.Value.transactions)
			{
				//var key = Merkle.transactionHasher.Invoke(transaction);

				//if (!_MainBlockStore.ContainsKey(_TransactionContext, key))
				//{
				//	return false;
				//}

				//if (tran

				foreach (Types.Outpoint input in transaction.inputs)
				{
					if (!ParentOutputExists(input))
					{
						BlockChainTrace.Information("parent output does not exist");
						return false;
					}

					if (ParentOutputSpent(input))
					{
						BlockChainTrace.Information("parent output spent");
						return false;
					}
				}
			}

			return true;
		}


		private bool ParentOutputExists(Types.Outpoint input)
		{
			//var refTx = input.txHash;

			//if (!_MainBlockStore.ContainsKey(_TransactionContext, refTx))
			//{
			//	return false;
			//}

			//var transaction = _MainBlockStore.Get(_TransactionContext, refTx).Value;

			//if (transaction


			//if (!_MainBlockStore.ContainsKey(
			//Keyed<Types.Transaction> parentTransaction = _MainBlockStore.;

			//switch (_InputLocations[input])
			//{
			//	case InputLocationEnum.Mempool:
			//		parentTransaction = _TxMempool.Get(input.txHash);
			//		break;
			//	case InputLocationEnum.TxStore:
			//		parentTransaction = _TxStore.Get(_TransactionContext, input.txHash);
			//		break;
			//}

			//if (input.index >= parentTransaction.Value.outputs.Length)
			//{
			//	BlockChainTrace.Information("Input index not found");
			//	return false;
			//}

			return true;
		}

		private bool ParentOutputSpent(Types.Outpoint input)
		{
			byte[] inputKey = Merkle.outpointHasher.Invoke(input);

			//if (_InputLocations[input] == InputLocationEnum.TxStore && !_UTXOStore.ContainsKey(_TransactionContext, inputKey))
			//{
			//	BlockChainTrace.Information("Output has been spent");
			//	return true;
			//}

			return false;
		}
	}
}
