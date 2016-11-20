using System;
using BlockChain.Database;

namespace BlockChain.Store
{
	public class BlockDifficultyTable : ValueStore<byte[], Double>
	{
		public BlockDifficultyTable() : base("bk-difficulty")
		{
		}
	}
}
