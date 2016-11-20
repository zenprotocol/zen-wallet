using System;
using Store;

namespace BlockChain.Store
{
	public class BlockDifficultyTable : ValueStore<byte[], Double>
	{
		public BlockDifficultyTable() : base("bk-difficulty")
		{
		}
	}
}
