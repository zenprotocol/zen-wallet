using NUnit.Framework;
using System;
using Infrastructure.Testing;
using Consensus;
using Wallet.core.Data;

namespace BlockChain
{
	[TestFixture()]
	public class BlockChainSimpleTests : BlockChainTestsBase
	{
		Key _Key;
		Types.Transaction _GenesisTx;

		[Test(), Order(1)]
		public void ShouldAddGenesisWithTx()
		{
			_Key = Key.Create();

			_GenesisTx = Utils.GetTx().AddOutput(_Key.Address, Consensus.Tests.zhash, 100);
			_GenesisBlock = _GenesisBlock.AddTx(_GenesisTx);
			Assert.That(_BlockChain.HandleBlock(_GenesisBlock), Is.True);
		}

		[Test(), Order(2)]
		public void ShouldAddToMempool()
		{
			var nonOrphanTx = Utils.GetTx().AddInput(_GenesisTx, 0, _Key.Private).Sign(_Key.Private);

			Assert.That(_BlockChain.HandleTransaction(nonOrphanTx), Is.True);
		}
	}
}
