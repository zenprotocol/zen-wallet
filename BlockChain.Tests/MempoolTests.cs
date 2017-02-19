using System;
using Infrastructure.Testing;
using NUnit.Framework;
using Wallet.core.Data;

namespace BlockChain
{
	public class MempoolTests : BlockChainTestsBase
	{
		[Test]
		public void ShouldRemoveUnorphanInvalidTx()
		{
			var tx = Utils.GetTx().Sign();
			var txInvalidOrphan = Utils.GetTx().AddInput(tx, 0);

			_BlockChain.HandleTransaction(txInvalidOrphan);
			Assert.That(_BlockChain.memPool.OrphanTxPool.ContainsKey(txInvalidOrphan.Key()), Is.True, "should be there");
			_BlockChain.HandleTransaction(tx);
			System.Threading.Thread.Sleep(50);
			Assert.That(_BlockChain.memPool.OrphanTxPool.ContainsKey(txInvalidOrphan.Key()), Is.False, "should not be there");
		}

		[Test]
		public void ShouldRemoveUnorphanInvalidTxWithDependencies()
		{
			var key = new Key();
			var tx = Utils.GetTx().Tag("tx");
			var txInvalidOrphan = Utils.GetTx().AddInput(tx, 0).AddOutput(key.Address, Consensus.Tests.zhash, 100).Tag("txInvalidOrphan");
			var txOrphanDepenent = Utils.GetTx().AddInput(txInvalidOrphan, 0).Tag("txOrphanDepenent");

			_BlockChain.HandleTransaction(txInvalidOrphan);
			_BlockChain.HandleTransaction(txOrphanDepenent);
			_BlockChain.HandleTransaction(tx);
			System.Threading.Thread.Sleep(50);
			Assert.That(_BlockChain.memPool.OrphanTxPool.ContainsKey(txInvalidOrphan.Key()), Is.False, "should not be there");
			Assert.That(_BlockChain.memPool.OrphanTxPool.ContainsKey(txOrphanDepenent.Key()), Is.False, "should not be there");
		}

		[Test]
		public void ShouldNotUnorphanDoubleSpend()
		{
			var key = new Key();
			var tx = Utils.GetTx().AddOutput(key.Address, Consensus.Tests.zhash, 100);
			var tx1 = Utils.GetTx().AddInput(tx, 0).AddOutput(key.Address, Consensus.Tests.zhash, 1);
			var tx2 = Utils.GetTx().AddInput(tx, 0).AddOutput(key.Address, Consensus.Tests.zhash, 2);

			_BlockChain.HandleTransaction(tx1);
			_BlockChain.HandleTransaction(tx2);
			_BlockChain.HandleTransaction(tx);
			System.Threading.Thread.Sleep(50);
			Assert.That(_BlockChain.memPool.OrphanTxPool.ContainsKey(tx1.Key()) &&
			            _BlockChain.memPool.OrphanTxPool.ContainsKey(tx2.Key()), Is.False, "both should not be there");
		}
	
		[Test]
		public void ShouldInvalidateDoubleSpendOnNewBlock()
		{
			var key = Key.Create();
			var tx = Utils.GetTx().AddOutput(Key.Create().Address, Consensus.Tests.zhash, 100).Sign(key.Private);
			var tx1 = Utils.GetTx().AddInput(tx, 0).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 1).Sign(key.Private);
			var tx2 = Utils.GetTx().AddInput(tx, 0).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 2).Sign(key.Private);

			BlockChainTrace.SetTag(tx, "tx");
			BlockChainTrace.SetTag(tx1, "tx1");
			BlockChainTrace.SetTag(tx2, "tx2");

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
			var tx = Utils.GetTx().AddOutput(Key.Create().Address, Consensus.Tests.zhash, 100).Sign(key.Private);
			var tx1 = Utils.GetTx().AddInput(tx, 0).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 1).Sign(key.Private);
			var tx2 = Utils.GetTx().AddInput(tx, 0).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 2).Sign(key.Private);
			var tx3 = Utils.GetTx().AddInput(tx2, 0).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 2).Sign(key.Private);

			BlockChainTrace.SetTag(tx, "tx");
			BlockChainTrace.SetTag(tx1, "tx1");
			BlockChainTrace.SetTag(tx2, "tx2");

			_BlockChain.HandleTransaction(tx);
			_BlockChain.HandleTransaction(tx1);
			_BlockChain.HandleTransaction(tx3);

			_BlockChain.HandleBlock(_GenesisBlock.AddTx(tx).AddTx(tx1));

			Assert.That(_BlockChain.memPool.TxPool.ContainsKey(tx1.Key()), Is.False, "should not be there");
			Assert.That(_BlockChain.memPool.TxPool.ContainsKey(tx2.Key()), Is.False, "should not be there");
			Assert.That(_BlockChain.memPool.TxPool.ContainsKey(tx3.Key()), Is.False, "should not be there");
		}
	}
}