using BlockChain.Store;
using NUnit.Framework;

namespace BlockChain
{
	[TestFixture()]
	public class BlockChainBranchTests : BlockChainTestsBase
	{
		[Test()]
		public void ShouldDetectBranch()
		{
			var block1 = _GenesisBlock.Child();
			var block2 = _GenesisBlock.Child();

			Assert.That(_BlockChain.HandleBlock(_GenesisBlock), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));
			Assert.That(_BlockChain.HandleBlock(block1), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));
			Assert.That(_BlockChain.HandleBlock(block2), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));

			Assert.That(Location(_GenesisBlock), Is.EqualTo(LocationEnum.Main));
			Assert.That(Location(block1), Is.EqualTo(LocationEnum.Main));

			// detect branching
			Assert.That(Location(block2), Is.EqualTo(LocationEnum.Branch));

			// detect branch child
			var block3 = block2.Child();
			Assert.That(_BlockChain.HandleBlock(block3), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));
			Assert.That(Location(block3), Is.EqualTo(LocationEnum.Main));
		}
	}
}
