using System;
using Consensus;
using NUnit.Framework;

namespace BlockChain
{
	[TestFixture()]
	public class BlockChainTipTests : BlockChainTestsBase
	{
		Types.Block block1;
		Types.Block block2;
		Types.Block block3;

		[Test, Order(1)]
		public void TipShouldBeNull()
		{
			Assert.That(_BlockChain.Tip, Is.Null);
		}

		[Test, Order(2)]
		public void TipShouldBeOfGenesis()
		{
			Assert.That(HandleBlock(_GenesisBlock), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));
			Assert.That(_BlockChain.Tip, Is.Not.Null);
			Assert.That(_BlockChain.Tip.Value, Is.EqualTo(_GenesisBlock));
		}

		[Test, Order(3)]
		public void TipShouldBeOfNewBlock()
		{
			block1 = _GenesisBlock.Child();
			Assert.That(HandleBlock(block1), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));
			Assert.That(_BlockChain.Tip.Value.Equals(block1), Is.True);
		}


		[Test, Order(4)]
		public void TipShouldNotBecomeNewBlock()
		{
			block2 = _GenesisBlock.Child();
			Assert.That(HandleBlock(block2), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));
			Assert.That(_BlockChain.Tip.Value.Equals(block1), Is.True);
		}

		[Test, Order(5)]
		public void TipShouldBecomeBranch()
		{
			block3 = block2.Child();
			Assert.That(HandleBlock(block3), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));
			Assert.That(_BlockChain.Tip.Value.Equals(block3), Is.True);
		}
	}
}