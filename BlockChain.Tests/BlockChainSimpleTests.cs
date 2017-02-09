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
			Assert.That(_BlockChain.TxMempool.ContainsKey(Merkle.transactionHasher.Invoke(nonOrphanTx)), Is.False);
		}


		[Test(), Order(2)]
		public void ShouldNotAddToMempoolOrphanTx()
		{
			var orphanTx = Utils.GetTx().AddInput(Utils.GetTx(), 0, _Key.Private).Sign(_Key.Private);

			Assert.That(_BlockChain.HandleTransaction(orphanTx), Is.True);
			Assert.That(_BlockChain.TxMempool.ContainsKey(Merkle.transactionHasher.Invoke(orphanTx)), Is.False);
		}


		[Test(), Order(3)]
		public void ShouldRejectBlock()
		{
			Assert.That(_BlockChain.HandleBlock(_GenesisBlock.Child().AddTx(_GenesisTx)), Is.False);
		}
	}
}
