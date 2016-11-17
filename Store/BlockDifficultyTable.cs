using System;
namespace Store
{
	public class BlockDifficultyTable : ValueStore<byte[], Double>
	{
		public BlockDifficultyTable() : base("bk-difficulty")
		{
		}
	}
}
