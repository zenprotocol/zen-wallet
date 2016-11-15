using NUnit.Framework;
using System;
using Consensus;
using System.Collections.Generic;
using System.Linq;

namespace Store.Tests
{
	[TestFixture()]
	public class Blockchain
	{
		[Test()]
		public void CanHandleNewValidBlock()
		{
			Types.Block newBlock = GetBlock();

			BlockChain blockChain = new BlockChain();

			blockChain.HandleNewValudBlock(newBlock);

			
		}

		private Types.Block GetBlock()
		{
			Types.Block newBlock = new Types.Block();

			newBlock.header = new Types.BlockHeader;
			newBlock.header.version = 1;
		}
	}
}
