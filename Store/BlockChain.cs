using System;
using Consensus;
using System.Linq;

namespace Store
{
	public class BlockChain : IDisposable
	{
		private readonly TransactionStore _TransactionStore;
		private readonly BlockStore _BlockStore;
		private readonly DBContext _DBContext;

		public BlockChain(string dbName)
		{
			_DBContext = new DBContext(dbName);
			_TransactionStore = new TransactionStore();
			_BlockStore = new BlockStore();
		}

		public void Dispose()
		{
			_DBContext.Dispose();
		}

		public void HandleNewValueBlock(Types.Block block, int blockTip) //TODO: remove blockTip from formal parama
		{
			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				var Tip = new Field<int>(context, "blockchain", "tip");

				_BlockStore.Put(context, block); //TODO: make sure only the header and list of tx(s) are stored
				_TransactionStore.Put(context, block.transactions.ToArray());

				if (blockTip > Tip.Value)
				{
					Tip.Value = blockTip;
				}

				context.Commit();
			}
		}
	}
}
