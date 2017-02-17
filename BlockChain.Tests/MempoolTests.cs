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
			var tx = Utils.GetTx();
			var txInvalidOrphan = Utils.GetTx().AddInput(tx, 0).AddOutput(key.Address, Consensus.Tests.zhash, 100);
			var txOrphanDepenent = Utils.GetTx().AddInput(txInvalidOrphan, 0);

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
	}
}