using System;
using Infrastructure.Testing;
using NUnit.Framework;
using Wallet.core.Data;

namespace BlockChain
{
	public class MempoolTests : BlockChainTestsBase
	{
		[SetUp]
		public void SetUp()
		{
			OneTimeSetUp();
		}

		[Test]
		public void ShouldRemoveUnorphanInvalidTx()
		{
			var tx = Utils.GetTx().Sign().Tag("tx");
			var txInvalidOrphan = Utils.GetTx().AddInput(tx, 0).Tag("txInvalidOrphan");

			_BlockChain.HandleTransaction(txInvalidOrphan);
			Assert.That(_BlockChain.memPool.OrphanTxPool.ContainsKey(txInvalidOrphan.Key()), Is.True, "should be there");
			_BlockChain.HandleTransaction(tx);

			System.Threading.Thread.Sleep(100); // todo use wait

			Assert.That(_BlockChain.memPool.OrphanTxPool.ContainsKey(txInvalidOrphan.Key()), Is.False, "should not be there");
		}

		[Test]
		public void ShouldRemoveUnorphanInvalidTxWithDependencies()
		{
			var key = new Key();
			var tx = Utils.GetTx().Tag("tx");
			var txInvalidOrphan = Utils.GetTx().AddInput(tx, 0).AddOutput(key.Address, Consensus.Tests.zhash, 100).Tag("Invalid Orphan");
			var txOrphanDepenent = Utils.GetTx().AddInput(txInvalidOrphan, 0).Tag("Orphan Depenent");

			_BlockChain.HandleTransaction(txInvalidOrphan);
			_BlockChain.HandleTransaction(txOrphanDepenent);
			_BlockChain.HandleTransaction(tx);

			System.Threading.Thread.Sleep(100); // todo use wait

			Assert.That(_BlockChain.memPool.OrphanTxPool.ContainsKey(txInvalidOrphan.Key()), Is.False, "should not be there");
			Assert.That(_BlockChain.memPool.OrphanTxPool.ContainsKey(txOrphanDepenent.Key()), Is.False, "should not be there");
		}

		[Test]
		public void ShouldNotUnorphanDoubleSpend()
		{
			var key = new Key();
			var tx = Utils.GetTx().AddOutput(key.Address, Consensus.Tests.zhash, 100).Tag("tx");
			var tx1 = Utils.GetTx().AddInput(tx, 0).AddOutput(key.Address, Consensus.Tests.zhash, 1).Tag("tx1");
			var tx2 = Utils.GetTx().AddInput(tx, 0).AddOutput(key.Address, Consensus.Tests.zhash, 2).Tag("tx2");

			_BlockChain.HandleTransaction(tx1);
			_BlockChain.HandleTransaction(tx2);
			_BlockChain.HandleTransaction(tx);

			Assert.That(_BlockChain.memPool.TxPool.ContainsKey(tx1.Key()) &&
			            _BlockChain.memPool.TxPool.ContainsKey(tx2.Key()), Is.False, "both should not be in mempool");
		}
	
		[Test]
		public void ShouldInvalidateDoubleSpendOnNewBlock()
		{
			var key = Key.Create();
			var tx = Utils.GetTx().AddOutput(Key.Create().Address, Consensus.Tests.zhash, 100).Sign(key.Private).Tag("tx");
			var tx1 = Utils.GetTx().AddInput(tx, 0).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 1).Sign(key.Private).Tag("tx1");
			var tx2 = Utils.GetTx().AddInput(tx, 0).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 2).Sign(key.Private).Tag("tx2");

			_BlockChain.HandleTransaction(tx);
			_BlockChain.HandleTransaction(tx2);
			_BlockChain.HandleBlock(_GenesisBlock.AddTx(tx).AddTx(tx1));

			Assert.That(_BlockChain.memPool.TxPool.ContainsKey(tx.Key()), Is.False, "should not be there");
			Assert.That(_BlockChain.memPool.TxPool.ContainsKey(tx1.Key()), Is.False, "should not be there");
			Assert.That(_BlockChain.memPool.TxPool.ContainsKey(tx2.Key()), Is.False, "should not be there");
		}

		[Test]
		public void ShouldInvalidateDoubleSpendOnNewBlockWithDependencies()
		{
			var key = Key.Create();
			var tx = Utils.GetTx().AddOutput(Key.Create().Address, Consensus.Tests.zhash, 100).Sign(key.Private).Tag("tx");
			var tx1 = Utils.GetTx().AddInput(tx, 0).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 1).Sign(key.Private).Tag("tx1");
			var tx2 = Utils.GetTx().AddInput(tx, 0).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 2).Sign(key.Private).Tag("tx2");
			var tx3 = Utils.GetTx().AddInput(tx2, 0).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 3).Sign(key.Private).Tag("tx3");

			_BlockChain.HandleTransaction(tx);
			_BlockChain.HandleTransaction(tx1);
			_BlockChain.HandleTransaction(tx2);
			_BlockChain.HandleTransaction(tx3);

			_BlockChain.HandleBlock(_GenesisBlock.AddTx(tx).AddTx(tx1));

			Assert.That(_BlockChain.memPool.TxPool.ContainsKey(tx1.Key()), Is.False, "should not be there");
			Assert.That(_BlockChain.memPool.TxPool.ContainsKey(tx2.Key()), Is.False, "should not be there");
			Assert.That(_BlockChain.memPool.TxPool.ContainsKey(tx3.Key()), Is.False, "should not be there");
		}
	}
}