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
		private readonly BlockStore _BlockStore;
		private readonly DBContext _DBContext;
		private readonly BlockDifficultyTable _BlockDifficultyTable;

		public event Action<Types.Transaction> OnAddedToMempool;

		public BlockChain(string dbName)
		{
			_DBContext = new DBContext(dbName);
			_TxMempool = new TxMempool();
			_TxStore = new TxStore();
			_BlockStore = new BlockStore();
			_BlockDifficultyTable = new BlockDifficultyTable();

			OwnResource(_DBContext);
			OwnResource(MessageProducer<TxMempool.AddedMessage>.Instance.AddMessageListener(
				new EventLoopMessageListener<TxMempool.AddedMessage>(m => {
					OnAddedToMempool(m.Transaction.Value);
				})
			));
		}

		public void HandleNewBlock(Types.Block block) //TODO: use Keyed type
		{
			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				var key = Merkle.blockHasher.Invoke(block); //TODO: id should be hash of block header, blockHasher may be redundant and wrong to use here

				_BlockStore.Put(context, block);
				_TxStore.Put(context, block.transactions.ToArray());
				_BlockDifficultyTable.Context(context)[key] = GetDifficultyRecursive(context, block);

				context.Commit();
			}
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


		private Double GetDifficultyRecursive(TransactionContext context, Types.Block block)
		{
			Double result = block.header.pdiff;

			if (block.header.parent == null || block.header.parent.Length == 0)
			{
				return result;
			}

			Types.Block parentBlock = _BlockStore.Get(context, block.header.parent).Value;

			if (parentBlock == null)
			{
				throw new Exception("Missing parent block");
			}

			return result + GetDifficultyRecursive(context, parentBlock);
		}
	}
}
