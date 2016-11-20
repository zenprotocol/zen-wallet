using System;
using Consensus;
using System.Linq;
using BlockChain.Store;
using Store;

namespace BlockChain
{
	public class BlockChain : IDisposable
	{
		private readonly TxMempool _TxMempool;
		private readonly TxStore _TxStore;
		private readonly BlockStore _BlockStore;
		private readonly DBContext _DBContext;
		private readonly BlockDifficultyTable _BlockDifficultyTable;

		public BlockChain(string dbName)
		{
			_DBContext = new DBContext(dbName);
			_TxMempool = new TxMempool();
			_TxStore = new TxStore();
			_BlockStore = new BlockStore();
			_BlockDifficultyTable = new BlockDifficultyTable();
		}

		public void Dispose()
		{
			_DBContext.Dispose();
		}

		public void HandleNewValueBlock(Types.Block block) //TODO: use Keyed type
		{
			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				var key = Merkle.blockHasher.Invoke(block); //TODO: id should be hash of block header, blockHasher may be redundant and wrong to use here

				_BlockStore.Put(context, block);
				_TxStore.Put(context, block.transactions.ToArray());
				_BlockDifficultyTable.Context(context)[key] = GetdifficultyRecursive(context, block);

				context.Commit();
			}
		}

		private Double GetdifficultyRecursive(TransactionContext context, Types.Block block)
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

			return result + GetdifficultyRecursive(context, parentBlock);
		}
	}
}
