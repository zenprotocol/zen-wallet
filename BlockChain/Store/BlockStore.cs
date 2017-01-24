using System;
using Consensus;
using System.Linq;
using Store;
using BlockChain.Data;
using System.Collections.Generic;

namespace BlockChain.Store
{
	public abstract class BlockStore : ConsensusTypeStore<Types.Block>
	{
		protected BlockStore(string blockStoreType) : base($"bk-{blockStoreType}")
		{
		}
	}

	//public class GenesisBlockStore : BlockStore
	//{
	//	public GenesisBlockStore() : base("genesis") { }
	//}

	public class MainBlockStore : BlockStore
	{
		public MainBlockStore() : base("main") { }
	}

	public class BranchBlockStore : BlockStore
	{
		public BranchBlockStore() : base("branch") { }
	}

	public class OrphanBlockStore : BlockStore
	{
		private const string BLOCK_REFS_TABLE = "bk-refs";

		public OrphanBlockStore() : base("orphan") { }

		public new void Put(TransactionContext transactionContext, Keyed<Types.Block> item) //TODO: used Keyed?
		{
			base.Put(transactionContext, item);

			var max = GetMaxIndex(transactionContext, item.Key);
			var newKey = GetSuffixedKey(item.Value.header.parent, max);

			transactionContext.Transaction.Insert<byte[], byte[]>(BLOCK_REFS_TABLE, newKey, item.Key);
		}

		public IEnumerable<Keyed<Types.Block>> OrphansOf(TransactionContext transactionContext, Keyed<Types.Block> item)
		{
			var result = new List<Keyed<Types.Block>>();

			foreach (var row in transactionContext.Transaction.SelectForwardStartFrom<byte[], byte[]>(BLOCK_REFS_TABLE, item.Key, true))
			{
				var baseKey = row.Key.Take(row.Key.Length - 1);

				if (!baseKey.SequenceEqual(item.Key))
				{
					break;
				}

				result.Add(Get(transactionContext, row.Value));
			}

			return result;
		}

		private int GetMaxIndex(TransactionContext transactionContext, byte[] key)
		{
			int count = 0;

			foreach (var row in transactionContext.Transaction.SelectForwardStartFrom<byte[], byte[]>(BLOCK_REFS_TABLE, key, true))
			{
				count++;
			}

			return count;
		}

		private byte[] GetSuffixedKey(byte[] baseKey, int index)
		{
			byte[] result = new byte[baseKey.Length + 1];
			baseKey.CopyTo(result, 0);
			result[baseKey.Length] = (byte)index;

			return result;
		}
	}
}