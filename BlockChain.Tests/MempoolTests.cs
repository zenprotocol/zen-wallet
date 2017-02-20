using System;
using BlockChain.Data;
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
			var key = Key.Create();
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
			var key = Key.Create();
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

		[Test]
		public void ShouldEvict()
		{
			var key = Key.Create();
			var tx = Utils
				.GetTx().AddOutput(Key.Create().Address, Consensus.Tests.zhash, 100)
				.Sign(key.Private).Tag("tx");
			var tx1 = Utils.GetTx().AddInput(tx, 0).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 1).Sign(key.Private).Tag("tx1");
			var tx2 = Utils.GetTx().AddInput(tx, 0).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 2).Sign(key.Private).Tag("tx2");

			_BlockChain.HandleBlock(_GenesisBlock.AddTx(tx).Tag("genesis"));
			Assert.That(TxState(tx), Is.EqualTo(TxStateEnum.Confirmed));
			_BlockChain.HandleBlock(_GenesisBlock.Child().AddTx(tx1).Tag("main"));
			Assert.That(TxState(tx1), Is.EqualTo(TxStateEnum.Confirmed));
			var branch = _GenesisBlock.Child().Tag("branch");
			_BlockChain.HandleBlock(branch.Child().Tag("branch orphan"));
			_BlockChain.HandleBlock(branch.AddTx(tx2).Tag("branch child"));
			System.Threading.Thread.Sleep(100);
			Assert.That(TxState(tx2), Is.EqualTo(TxStateEnum.Confirmed));
			Assert.That(TxState(tx1), Is.EqualTo(TxStateEnum.Invalid));
			Assert.That(_BlockChain.memPool.TxPool.ContainsKey(tx1.Key()), Is.False);
		}

		[Test]
		public void ShouldNotEvictDoubleSpend()
		{
			var key = Key.Create();
			var tx = Utils
				.GetTx().AddOutput(Key.Create().Address, Consensus.Tests.zhash, 100)
				.Sign(key.Private).Tag("tx");
			var cannotEvict = Utils.GetTx().AddInput(tx, 0).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 1).Sign(key.Private)
			                     .Tag("cannotEvict");
			var invalidatingTx = Utils.GetTx().AddInput(tx, 0).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 2).Sign(key.Private)
			                         .Tag("invalidatingTx");

			_BlockChain.HandleBlock(_GenesisBlock.AddTx(tx).Tag("genesis"));
			Assert.That(TxState(tx), Is.EqualTo(TxStateEnum.Confirmed));
			_BlockChain.HandleBlock(_GenesisBlock.Child().AddTx(cannotEvict).Tag("main"));
			Assert.That(TxState(cannotEvict), Is.EqualTo(TxStateEnum.Confirmed));
			var branch = _GenesisBlock.Child().Tag("branch");
			_BlockChain.HandleBlock(branch.Child().AddTx(invalidatingTx).Tag("branch orphan"));
			_BlockChain.HandleBlock(branch.Tag("branch child"));
			System.Threading.Thread.Sleep(100);
			Assert.That(TxState(cannotEvict), Is.EqualTo(TxStateEnum.Invalid));
			Assert.That(TxState(invalidatingTx), Is.EqualTo(TxStateEnum.Confirmed));
			Assert.That(_BlockChain.memPool.TxPool.Count, Is.EqualTo(0));
		}
	}
}