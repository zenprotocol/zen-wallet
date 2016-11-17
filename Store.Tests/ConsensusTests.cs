using NUnit.Framework;
using System;
using Consensus;
using BlockChain.Data;

namespace BlockChain.Tests
{
	[TestFixture()]
	public class ConsensusTests
	{
		[Test()]
		public void TestCase()
		{
			Types.Block block = Util.GetBlock(null, 0);

			var data = Merkle.serialize<Types.Block>(block);

			Types.Block _block = Serialization.context.GetSerializer<Types.Block>().UnpackSingleObject(data);

			Assert.IsTrue(block.Equals(_block));
		}
	}
}
