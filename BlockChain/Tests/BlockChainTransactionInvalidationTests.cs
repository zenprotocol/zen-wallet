using Consensus;
using BlockChain.Store;
using Store;
using NUnit.Framework;
using BlockChain.Data;

namespace BlockChain.Tests
{
	[TestFixture()]
	public class BlockChainTransactionInvalidationTests : BlockChainTestsBase
	{
		[OneTimeSetUp]
		public new void OneTimeSetUp()
		{
			base.OneTimeSetUp();

			var newTx = GetTx(_GenesisTx);
			Assert.That(_BlockChain.HandleNewBlock(_GenesisBlock.Value), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.HandleNewTransaction(newTx.Value), Is.EqualTo(AddBk.Result.Added));
		}

		[Test, Order(1)]
		public void ShouldEvictFromMempool()
		{
			var newTx = GetTx(_GenesisTx);
			var newBlock = GetBlock(_GenesisBlock, newTx);
			Assert.That(_BlockChain.HandleNewBlock(newBlock.Value), Is.EqualTo(AddBk.Result.Added));

			var sideChainBlock = GetBlock(_GenesisBlock);
			Assert.That(_BlockChain.HandleNewBlock(sideChainBlock.Value), Is.EqualTo(AddBk.Result.Added));
		
			var sideChainExtendingBlock = GetBlock(sideChainBlock);
			Assert.That(_BlockChain.HandleNewBlock(sideChainExtendingBlock.Value), Is.EqualTo(AddBk.Result.Added));

			Assert.That(Location(newBlock), Is.EqualTo(LocationEnum.Branch));
			Assert.That(Location(sideChainBlock), Is.EqualTo(LocationEnum.Main));
			Assert.That(Location(sideChainExtendingBlock), Is.EqualTo(LocationEnum.Main));
		}
	}
}
