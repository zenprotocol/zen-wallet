using Consensus;
using BlockChain.Store;
using Store;
using NUnit.Framework;
using BlockChain.Data;
using Infrastructure.Testing;
using System;

namespace BlockChain.Tests
{
	[TestFixture()]
	public class BlockChainTransactionInvalidationTests : BlockChainTestsBase
	{
		[Test, Order(1)]
		public void ShouldSetup()
		{
			var key = new byte[32];
			new Random().NextBytes(key);

			_GenesisTx = _GenesisTx.AddOutput(key, Consensus.Tests.zhash, 100);

			var newTx = Utils.GetTx().AddInput(_GenesisTx, 0);

			Assert.That(_BlockChain.HandleNewBlock(_GenesisBlock.Value), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.HandleNewTransaction(newTx), Is.EqualTo(AddBk.Result.Added));
		}

		[Test, Order(2)]
		public void ShouldEvictFromMempool()
		{
			var newTx = Utils.GetTx().AddInput(_GenesisTx, 0);

			var newBlock = _GenesisBlock.Value.Child().AddTx(newTx);
			Assert.That(_BlockChain.HandleNewBlock(newBlock), Is.EqualTo(AddBk.Result.Added));

			var sideChainBlock = _GenesisBlock.Value.Child();
			Assert.That(_BlockChain.HandleNewBlock(sideChainBlock), Is.EqualTo(AddBk.Result.Added));
		
			var sideChainExtendingBlock = sideChainBlock.Child();
			Assert.That(_BlockChain.HandleNewBlock(sideChainExtendingBlock), Is.EqualTo(AddBk.Result.Added));

			Assert.That(Location(newBlock), Is.EqualTo(LocationEnum.Branch));
			Assert.That(Location(sideChainBlock), Is.EqualTo(LocationEnum.Main));
			Assert.That(Location(sideChainExtendingBlock), Is.EqualTo(LocationEnum.Main));
		}
	}
}
