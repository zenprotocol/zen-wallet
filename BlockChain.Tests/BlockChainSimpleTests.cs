using NUnit.Framework;
using System;
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

            _GenesisTx = Utils.GetTx().AddOutput(_Key.Address, Consensus.Tests.zhash, 100).Tag("genesis tx");
            _GenesisBlock = _GenesisBlock.AddTx(_GenesisTx);
			Assert.That(HandleBlock(_GenesisBlock), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));
		}

		[Test, Order(2)]
		public void ShouldAddToMempool_NonOrphanTx()
		{
			var nonOrphanTx = Utils.GetTx().AddInput(_GenesisTx, 0).Sign(_Key.Private);

			Assert.That(HandleTransaction(nonOrphanTx), Is.EqualTo(BlockChain.TxResultEnum.Accepted));
			Assert.That(_BlockChain.memPool.TxPool.Contains(Merkle.transactionHasher.Invoke(nonOrphanTx)), Is.True);
		}

		[Test, Order(3)]
		public void ShouldNotAddToMempool_OrphanTx()
		{
            var orphanTx = Utils.GetTx().AddInput(Utils.GetTx().AddOutput(Key.Create().Address, Consensus.Tests.zhash, 1), 1).Sign(_Key.Private);

			Assert.That(HandleTransaction(orphanTx), Is.EqualTo(BlockChain.TxResultEnum.Orphan));
			Assert.That(_BlockChain.memPool.TxPool.Contains(Merkle.transactionHasher.Invoke(orphanTx)), Is.False);
		}

		[Test, Order(4)]
		public void ShouldRejectBlock()
		{
			Assert.That(HandleBlock(_GenesisBlock.Child().AddTx(_GenesisTx)), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Rejected));
		}

		[Test, Order(6)]
		public void ShouldValidateInterdependentTxs()
		{
			var key1 = Key.Create();
            var tx1 = Utils.GetTx().AddOutput(key1.Address, Consensus.Tests.zhash, 10).Tag("tx1");

			var key2 = Key.Create();
			var tx2 = Utils.GetTx().AddInput(tx1, 0).AddOutput(key2.Address, Consensus.Tests.zhash, 11).Sign(key1.Private).Tag("tx2");

			var tx3 = Utils.GetTx().AddInput(tx2, 0).Sign(key2.Private).Tag("tx3");

			var bk = _GenesisBlock.Child().AddTx(tx1).AddTx(tx2).AddTx(tx3).Tag("bk");

			Assert.That(HandleBlock(bk), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));
		}
	}
}
