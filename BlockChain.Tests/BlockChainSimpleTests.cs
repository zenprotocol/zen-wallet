using NUnit.Framework;
using System;
using Infrastructure.Testing;
using Consensus;
using Wallet.core.Data;

namespace BlockChain
{
	public class BlockChainSimpleTests : BlockChainTestsBase
	{
		Key _Key;
		Types.Transaction _GenesisTx;

		[Test, Order(1)]
		public void ShouldAddGenesisWithTx()
		{
			_Key = Key.Create();

			_GenesisTx = Utils.GetTx().AddOutput(_Key.Address, Consensus.Tests.zhash, 100);
			_GenesisBlock = _GenesisBlock.AddTx(_GenesisTx);
			Assert.That(_BlockChain.HandleBlock(_GenesisBlock), Is.True);
		}

		[Test, Order(2)]
		public void ShouldAddToMempool()
		{
			var nonOrphanTx = Utils.GetTx().AddInput(_GenesisTx, 0, _Key.Private).Sign(_Key.Private);

			Assert.That(_BlockChain.HandleTransaction(nonOrphanTx), Is.True);
			Assert.That(_BlockChain.memPool.TxPool.Contains(Merkle.transactionHasher.Invoke(nonOrphanTx)), Is.True);
		}

		[Test, Order(2)]
		public void ShouldNotAddToMempoolOrphanTx()
		{
			var orphanTx = Utils.GetTx().AddInput(Utils.GetTx(), 0, _Key.Private).Sign(_Key.Private);

			Assert.That(_BlockChain.HandleTransaction(orphanTx), Is.True);
			Assert.That(_BlockChain.memPool.TxPool.Contains(Merkle.transactionHasher.Invoke(orphanTx)), Is.False);
		}

		[Test, Order(3)]
		public void ShouldRejectBlock()
		{
			Assert.That(_BlockChain.HandleBlock(_GenesisBlock.Child().AddTx(_GenesisTx)), Is.False);
		}

		[Test, Order(4)]
		public void ShouldValidateInterdependentTxs()
		{
			var key1 = Key.Create();
			var tx1 = Utils.GetTx().AddOutput(key1.Address, Consensus.Tests.zhash, 10);

			var key2 = Key.Create();
			var tx2 = Utils.GetTx().AddInput(tx1, 0, key1.Private).AddOutput(key2.Address, Consensus.Tests.zhash, 11);

			var tx3 = Utils.GetTx().AddInput(tx2, 0, key2.Private);

			var bk = _GenesisBlock.Child().AddTx(tx1).AddTx(tx2).AddTx(tx3);

			Assert.That(_BlockChain.HandleBlock(bk), Is.True);
		}
	}
}
