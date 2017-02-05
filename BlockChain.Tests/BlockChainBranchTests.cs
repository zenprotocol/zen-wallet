using BlockChain.Store;
using NUnit.Framework;
using Infrastructure.Testing;
namespace BlockChain.Tests
{
	[TestFixture()]
	public class BlockChainBranchTests : BlockChainTestsBase
	{
		[Test()]
		public void ShouldDetectBranch()
		{
			var block1 = _GenesisBlock.Child();
			var block2 = _GenesisBlock.Child();

			Assert.That(_BlockChain.HandleNewBlock(_GenesisBlock), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.HandleNewBlock(block1), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.HandleNewBlock(block2), Is.EqualTo(AddBk.Result.Added));

			Assert.That(Location(_GenesisBlock), Is.EqualTo(LocationEnum.Main));
			Assert.That(Location(block1), Is.EqualTo(LocationEnum.Main));

			// detect branching
			Assert.That(Location(block2), Is.EqualTo(LocationEnum.Branch));

			// detect branch child
			var block3 = block2.Child();
			Assert.That(_BlockChain.HandleNewBlock(block3), Is.EqualTo(AddBk.Result.Added));
			Assert.That(Location(block3), Is.EqualTo(LocationEnum.Main));
		}
	}
	
}
