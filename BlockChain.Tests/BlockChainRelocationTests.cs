using Consensus;
using BlockChain.Store;
using NUnit.Framework;

namespace BlockChain
{
	[TestFixture()]
	public class BlockChainRelocationTests : BlockChainTestsBase
	{
		private Types.Block block1;
		private Types.Block block2;
		private Types.Block block3;
		Types.Transaction tx = Utils.GetTx();

		[OneTimeSetUp]
		public new void OneTimeSetUp()
		{
			base.OneTimeSetUp();
			block1 = _GenesisBlock.Child().AddTx(tx).Tag("block1");
			block2 = _GenesisBlock.Child().AddTx(tx).Tag("block2");
			block3 = block2.Child().Tag("block3");
		}

		[Test, Order(1)]
		public void ShouldAddBlocks()
		{
			Assert.That(_BlockChain.HandleBlock(_GenesisBlock), Is.True);
			Assert.That(Location(_GenesisBlock), Is.EqualTo(LocationEnum.Main));
			Assert.That(_BlockChain.Tip.Value, Is.EqualTo(_GenesisBlock));

			Assert.That(_BlockChain.HandleBlock(block1), Is.True);
			Assert.That(Location(block1), Is.EqualTo(LocationEnum.Main));
			Assert.That(_BlockChain.Tip.Value.Equals(block1), Is.True);

			Assert.That(_BlockChain.HandleBlock(block2), Is.True);
			Assert.That(Location(block2), Is.EqualTo(LocationEnum.Branch));
			Assert.That(_BlockChain.Tip.Value.Equals(block1), Is.True);
		}

		[Test, Order(2)]
		public void ShouldReorganize()
		{
			Assert.That(_BlockChain.HandleBlock(block3), Is.True);

			Assert.That(Location(block1), Is.EqualTo(LocationEnum.Branch));
			Assert.That(Location(block2), Is.EqualTo(LocationEnum.Main));
			Assert.That(Location(block3), Is.EqualTo(LocationEnum.Main));
			Assert.That(_BlockChain.Tip.Value.Equals(block3), Is.True);
		}

		[Test, Order(3)]
		public void ShouldUndoReorganize()
		{
			var branch = _GenesisBlock.Child().Tag("branch");
			var branchExtend = branch.Child().Tag("branchExtend");
			var branchExtendInvalid = branchExtend.Child().AddTx(tx).Tag("branchExtendInvalid");

			Assert.That(_BlockChain.HandleBlock(branchExtendInvalid), Is.True); //TODO: assert: orphan
			Assert.That(_BlockChain.HandleBlock(branchExtend), Is.True); //TODO: assert: orphan
			Assert.That(_BlockChain.HandleBlock(branch), Is.True);
		}
	}
}
