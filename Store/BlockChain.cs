using System;
using Consensus;
using System.Linq;

namespace Store
{
	public class BlockChain : IDisposable
	{
		private readonly TxMempool _TxMempool;
		private readonly TxStore _TxStore;
		private readonly BlockStore _BlockStore;
		private readonly DBContext _DBContext;
		private const string BLOCK_DIFFICULTY_TABLE = "bk-difficulty";

		public BlockChain(string dbName)
		{
			_DBContext = new DBContext(dbName);
			_TxMempool = new TxMempool();
			_TxStore = new TxStore();
			_BlockStore = new BlockStore();
		}

		public void Dispose()
		{
			_DBContext.Dispose();
		}

		public void HandleNewValueBlock(Types.Block block)
		{
			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				var Tip = new Field<string, int>(context, "blockchain", "tip");

				var key = Merkle.blockHasher.Invoke(block); //TODO: id should be hash of block header, blockHasher may be redundant and wrong to use here
				var Difficulty = new Field<byte[], int>(context, BLOCK_DIFFICULTY_TABLE, key);

				_BlockStore.Put(context, block);
				_TxStore.Put(context, block.transactions.ToArray());

				//if (blockTip > Tip.Value)
				//{
				//	Tip.Value = blockTip;
				//}

				context.Commit();
			}
		}

		private Double Getdifficulty(TransactionContext context, Types.Block block, Double difficulty)
		{
			return GetdifficultyRecursive(context, block, 0);
		}

		private Double GetdifficultyRecursive(TransactionContext context, Types.Block block, Double difficulty)
		{
			return difficulty +
				block.header.pdiff +
					 (block.header.parent != null ? GetdifficultyRecursive(context, _BlockStore.Get(context, block.header.parent), difficulty) : 0);
		}
	}
}
