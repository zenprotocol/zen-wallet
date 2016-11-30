using System;
using Consensus;
using System.Linq;
using Store;
using BlockChain.Data;

namespace BlockChain.Store
{
	public abstract class BlockStore : ConsensusTypeStore<Types.Block>
	{
		protected BlockStore(string blockStoreType) : base($"bk-{blockStoreType}")
		{
		}
	}

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
		public OrphanBlockStore() : base("orphan") { }
	}
}