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
		Types.Transaction _GenesisTx;
		byte[] _Key;

		[Test, Order(1)]
		public void ShouldSetup()
		{
			new Random().NextBytes(_Key);

			_GenesisTx = Utils.GetTx().AddOutput(_Key, Consensus.Tests.zhash, 100);
			_GenesisBlock = _GenesisBlock.AddTx(_GenesisTx);

			var newTx = Utils.GetTx().AddInput(_GenesisTx, 0, _Key);

			Assert.That(_BlockChain.HandleNewBlock(_GenesisBlock), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.HandleNewTransaction(newTx), Is.EqualTo(AddBk.Result.Added));
		}

		[Test, Order(2)]
		public void ShouldEvictFromMempool()
		{
			var newTx = Utils.GetTx().AddInput(_GenesisTx, 0, _Key);

			var newBlock = _GenesisBlock.Child().AddTx(newTx);
			Assert.That(_BlockChain.HandleNewBlock(newBlock), Is.EqualTo(AddBk.Result.Added));

			var sideChainBlock = _GenesisBlock.Child();
			Assert.That(_BlockChain.HandleNewBlock(sideChainBlock), Is.EqualTo(AddBk.Result.Added));
		
			var sideChainExtendingBlock = sideChainBlock.Child();
			Assert.That(_BlockChain.HandleNewBlock(sideChainExtendingBlock), Is.EqualTo(AddBk.Result.Added));

			Assert.That(Location(newBlock), Is.EqualTo(LocationEnum.Branch));
			Assert.That(Location(sideChainBlock), Is.EqualTo(LocationEnum.Main));
			Assert.That(Location(sideChainExtendingBlock), Is.EqualTo(LocationEnum.Main));
		}
	}
}
