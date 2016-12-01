using System;
using Consensus;
using System.Linq;
using BlockChain.Store;
using Store;
using Infrastructure;

namespace BlockChain
{
	public class BlockChain : ResourceOwner
	{
		private readonly TxMempool _TxMempool;
		private readonly TxStore _TxStore;
		private readonly BlockStore _MainBlockStore;
		private readonly BlockStore _BranchBlockStore;
		private readonly BlockStore _OrphanBlockStore;
		private readonly DBContext _DBContext;
		private readonly BlockDifficultyTable _BlockDifficultyTable;

		public event Action<Types.Transaction> OnAddedToMempool;

		public BlockChain(string dbName)
		{
			_DBContext = new DBContext(dbName);
			_TxMempool = new TxMempool();
			_TxStore = new TxStore();
			_MainBlockStore = new MainBlockStore();
			_BranchBlockStore = new BranchBlockStore();
			_OrphanBlockStore = new OrphanBlockStore();
			_BlockDifficultyTable = new BlockDifficultyTable();

			OwnResource(_DBContext);
			OwnResource(MessageProducer<TxMempool.AddedMessage>.Instance.AddMessageListener(
				new EventLoopMessageListener<TxMempool.AddedMessage>(m => {
					OnAddedToMempool(m.Transaction.Value);
				})
			));
		}

		public bool HandleNewBlock(Types.Block block) //TODO: use Keyed type
		{
			var _block = new Keyed<Types.Block>(Merkle.blockHasher.Invoke(block), block);

			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				//var result = new BlockChainAddBlockOperation(
				//	context,
				//	new Keyed<Types.Block>(Merkle.blockHasher.Invoke(block), block),
				//	_TxMempool
				//).Start();

				//return result;

				//if (_MainBlockStore.ContainsKey(context, _block.Key) ||
				//	_BranchBlockStore.ContainsKey(context, _block.Key) ||
				//	_OrphanBlockStore.ContainsKey(context, _block.Key))
				//{
				//	return HandleNewBlockResult.Rejected;
				//}

				//var parent = _block.Value.header.parent;

				//if (!_MainBlockStore.ContainsKey(context, parent) &&
				//	!_BranchBlockStore.ContainsKey(context, parent))
				//{
				//	_OrphanBlockStore.Put(context, _block);
				//	context.Commit();
				//	return HandleNewBlockResult.AddedOrpan;
				//}
				//else 
				//{
				//	_MainBlockStore.Put(context, _block);
				//	//_TxStore.Put(context, block.transactions.ToArray());
				//	////TODO: fix that. difficulty is not computed recursively
				//	//_BlockDifficultyTable.Context(context)[key] = GetDifficultyRecursive(context, block);

				//	context.Commit();

				//	return HandleNewBlockResult.Accepeted;
				//}
			}

			return false;
		}

		public bool HandleNewTransaction(Types.Transaction transaction) //TODO: use Keyed type
		{
			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				var result = new BlockChainAddTransactionOperation(
					context,
					new Keyed<Types.Transaction>(Merkle.transactionHasher.Invoke(transaction), transaction),
					_TxMempool
				).Start();

				return result != BlockChainAddTransactionOperation.Result.Rejected;
				//switch (result)
				//{
				//	case BlockChainAddTransactionOperation.Result.Added:
				//		break;
				//	case BlockChainAddTransactionOperation.Result.AddedOrphaned:
				//		break;
				//	case BlockChainAddTransactionOperation.Result.Rejected:
				//		break;
				//}
			}
		}


		//private Double GetDifficultyRecursive(TransactionContext context, Types.Block block)
		//{
		//	Double result = block.header.pdiff;

		//	if (block.header.parent == null || block.header.parent.Length == 0)
		//	{
		//		return result;
		//	}

		//	Types.Block parentBlock = _BlockStore.Get(context, block.header.parent).Value;

		//	if (parentBlock == null)
		//	{
		//		throw new Exception("Missing parent block");
		//	}

		//	return result + GetDifficultyRecursive(context, parentBlock);
		//}
	}
}
