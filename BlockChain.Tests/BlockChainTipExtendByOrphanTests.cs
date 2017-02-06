using System;
using Consensus;
using Store;
using Infrastructure.Testing;
using NUnit.Framework;

namespace BlockChain
{
	[TestFixture()]
	public class BlockChainTipExtendByOrphanTests : BlockChainTestsBase
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
			Assert.That(_BlockChain.HandleNewBlock(_GenesisBlock), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.Tip.Value.Equals(_GenesisBlock), Is.True);
		}

		[Test, Order(3)]
		public void TipShouldBeOfNewBlock()
		{
			block1 = _GenesisBlock.Child();
			Assert.That(_BlockChain.HandleNewBlock(block1), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.Tip.Value.Equals(block1), Is.True);
		}

		[Test, Order(4)]
		public void TipShouldNotBecomeNewBlock()
		{
			block2 = _GenesisBlock.Child();
			block3 = block2.Child();
			Assert.That(_BlockChain.HandleNewBlock(block3), Is.EqualTo(AddBk.Result.AddedOrphan));
			Assert.That(_BlockChain.Tip.Value.Equals(block1), Is.True);
		}

		[Test, Order(5)]
		public void TipShouldBecomeBranchAfterReorganization()
		{
			Assert.That(_BlockChain.HandleNewBlock(block2), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.Tip.Value.Equals(block3), Is.True);
		}
	}
}