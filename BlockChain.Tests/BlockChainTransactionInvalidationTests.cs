using Consensus;
using NUnit.Framework;
using Infrastructure.Testing;
using System.Linq;
using Wallet.core.Data;

namespace BlockChain
{
	[TestFixture()]
	public class BlockChainTransactionInvalidationTests : BlockChainTestsBase
	{
		[SetUp]
		public void SetUp()
		{
			OneTimeSetUp();
		}

		[Test]
		public void ShouldEvictFromMempoolOnNewBlock()
		{
			var key = Key.Create();

			var genesisTx = Utils.GetTx().AddOutput(key.Address, Consensus.Tests.zhash, 100);
			_GenesisBlock = _GenesisBlock.AddTx(genesisTx);

			var txToBeEvictedFromMempool = Utils.GetTx().AddInput(genesisTx, 0, key.Address).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 1);

			Assert.That(_BlockChain.HandleBlock(_GenesisBlock), Is.True);
			Assert.That(_BlockChain.HandleTransaction(txToBeEvictedFromMempool), Is.True);
			Assert.That(_BlockChain.pool.ContainsKey(Merkle.transactionHasher.Invoke(txToBeEvictedFromMempool)), Is.True);

			var txConflicting = Utils.GetTx().AddInput(genesisTx, 0, key.Address).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 1);
			Assert.That(_BlockChain.HandleBlock(_GenesisBlock.Child().AddTx(txConflicting)), Is.True);

			System.Threading.Thread.Sleep(1000);
			Assert.That(_BlockChain.pool.ContainsKey(Merkle.transactionHasher.Invoke(txToBeEvictedFromMempool)), Is.False);
		}

		[Test]
		public void ShouldUndoBlockOnReorganization()
		{
			var key = Key.Create();

			var genesisTx = Utils.GetTx().AddOutput(key.Address, Consensus.Tests.zhash, 50).AddOutput(key.Address, Consensus.Tests.zhash, 50);
			_GenesisBlock = _GenesisBlock.AddTx(genesisTx);
			Assert.That(_BlockChain.HandleBlock(_GenesisBlock), Is.True);

			var output1_withconflict = Utils.GetOutput(Key.Create().Address, Consensus.Tests.zhash, 5);
			var tx1_withconflict = Utils.GetTx().AddInput(genesisTx, 0, key.Address).AddOutput(output1_withconflict);
			var output1_withoutconflict = Utils.GetOutput(Key.Create().Address, Consensus.Tests.zhash, 5);
			var tx1_withoutconflict = Utils.GetTx().AddInput(genesisTx, 1, key.Address).AddOutput(output1_withoutconflict);
			Assert.That(_BlockChain.HandleBlock(_GenesisBlock.Child().AddTx(tx1_withconflict).AddTx(tx1_withoutconflict)), Is.True);

			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1_withconflict), Is.True);
			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1_withoutconflict), Is.True);

			var output2 = Utils.GetOutput(Key.Create().Address, Consensus.Tests.zhash, 5);
			var tx2_withconflict = Utils.GetTx().AddInput(genesisTx, 0, key.Address).AddOutput(output2);
			var sideChainBlock = _GenesisBlock.Child().AddTx(tx2_withconflict);
			Assert.That(_BlockChain.HandleBlock(sideChainBlock), Is.True);

			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1_withconflict), Is.True);
			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1_withoutconflict), Is.True);
			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output2), Is.False);

			Assert.That(_BlockChain.HandleBlock(sideChainBlock.Child()), Is.True);

			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1_withconflict), Is.False);
			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1_withoutconflict), Is.False);
			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output2), Is.True);

			Assert.That(_BlockChain.pool.ContainsKey(Merkle.transactionHasher.Invoke(tx1_withconflict)), Is.False);
			Assert.That(_BlockChain.pool.ContainsKey(Merkle.transactionHasher.Invoke(tx1_withoutconflict)), Is.True);
		}

		[Test]
		public void ShouldNotPutInvalidatedTxIntoMempoolWhenReorganizing()
		{
			var key = Key.Create();

			var genesisTx = Utils.GetTx().AddOutput(key.Address, Consensus.Tests.zhash, 100);
			_GenesisBlock = _GenesisBlock.AddTx(genesisTx);

			Assert.That(_BlockChain.HandleBlock(_GenesisBlock), Is.True);

			var output1 = Utils.GetOutput(Key.Create().Address, Consensus.Tests.zhash, 1);
			var tx1 = Utils.GetTx().AddInput(genesisTx, 0, key.Address).AddOutput(output1);

			var output2 = Utils.GetOutput(Key.Create().Address, Consensus.Tests.zhash, 2);
			var tx2 = Utils.GetTx().AddInput(genesisTx, 0, key.Address).AddOutput(output2);

			Assert.That(_BlockChain.HandleBlock(_GenesisBlock.Child().AddTx(tx1)), Is.True);

			TestDelegate x = delegate
			{
				Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1), Is.True);
				Assert.That(_BlockChain.pool.ContainsKey(Merkle.transactionHasher.Invoke(tx1)), Is.False);
				Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output2), Is.False);
				Assert.That(_BlockChain.pool.ContainsKey(Merkle.transactionHasher.Invoke(tx2)), Is.False);
			};

			x();

			var branch = _GenesisBlock.Child().AddTx(tx2);
			Assert.That(_BlockChain.HandleBlock(branch), Is.True);

			x();

			Assert.That(_BlockChain.HandleBlock(branch.Child()), Is.True); // reorganize

			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1), Is.False);
			Assert.That(_BlockChain.pool.ContainsKey(Merkle.transactionHasher.Invoke(tx1)), Is.True);
			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output2), Is.True);
			Assert.That(_BlockChain.pool.ContainsKey(Merkle.transactionHasher.Invoke(tx2)), Is.False);
		}

		[Test]
		public void ShouldUndoBlockOnReorganizationWithOrphanBlock()
		{
			var key = Key.Create();

			var genesisTx = Utils.GetTx().AddOutput(key.Address, Consensus.Tests.zhash, 50).AddOutput(key.Address, Consensus.Tests.zhash, 50);
			_GenesisBlock = _GenesisBlock.AddTx(genesisTx);
			Assert.That(_BlockChain.HandleBlock(_GenesisBlock), Is.True);

			var output1_withconflict = Utils.GetOutput(Key.Create().Address, Consensus.Tests.zhash, 5);
			var tx1_withconflict = Utils.GetTx().AddInput(genesisTx, 0, key.Address).AddOutput(output1_withconflict);
			var output1_withoutconflict = Utils.GetOutput(Key.Create().Address, Consensus.Tests.zhash, 5);
			var tx1_withoutconflict = Utils.GetTx().AddInput(genesisTx, 1, key.Address).AddOutput(output1_withoutconflict);
			Assert.That(_BlockChain.HandleBlock(_GenesisBlock.Child().AddTx(tx1_withconflict).AddTx(tx1_withoutconflict)), Is.True);

			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1_withconflict), Is.True);
			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1_withoutconflict), Is.True);

			var output2 = Utils.GetOutput(Key.Create().Address, Consensus.Tests.zhash, 5);
			var tx2_withconflict = Utils.GetTx().AddInput(genesisTx, 0, key.Address).AddOutput(output2);
			var sideChainBlock = _GenesisBlock.Child().AddTx(tx2_withconflict);


			Assert.That(_BlockChain.HandleBlock(sideChainBlock.Child()), Is.True); //TODO: assert: orphan

			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1_withconflict), Is.True);
			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1_withoutconflict), Is.True);
			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output2), Is.False);

			Assert.That(_BlockChain.HandleBlock(sideChainBlock), Is.True);

			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1_withconflict), Is.False);
			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1_withoutconflict), Is.False);
			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output2), Is.True);

			Assert.That(_BlockChain.pool.ContainsKey(Merkle.transactionHasher.Invoke(tx1_withconflict)), Is.False);
			Assert.That(_BlockChain.pool.ContainsKey(Merkle.transactionHasher.Invoke(tx1_withoutconflict)), Is.True);
		}

		[Test]
		public void ShouldUndoReorganization()
		{ 
			var key = Key.Create();

			var genesisTx = Utils.GetTx().AddOutput(key.Address, Consensus.Tests.zhash, 100);
			_GenesisBlock = _GenesisBlock.AddTx(genesisTx);

			var output1 = Utils.GetOutput(Key.Create().Address, Consensus.Tests.zhash, 5);
			var tx1 = Utils.GetTx().AddInput(genesisTx, 0, key.Address).AddOutput(output1);

			var output2 = Utils.GetOutput(Key.Create().Address, Consensus.Tests.zhash, 5);
			var tx2 = Utils.GetTx().AddInput(genesisTx, 0, key.Address).AddOutput(output2);

			Assert.That(_BlockChain.HandleBlock(_GenesisBlock), Is.True);
			Assert.That(_BlockChain.HandleBlock(_GenesisBlock.Child().AddTx(tx1)), Is.True);

			var branch = _GenesisBlock.Child().AddTx(tx2);
			Assert.That(_BlockChain.HandleBlock(branch), Is.True);


			//TODO: ???????
			Assert.That(_BlockChain.HandleBlock(branch.Child().AddTx(genesisTx)), Is.False); // should undo reorganization

			Assert.That(_BlockChain.pool.ContainsKey(Merkle.transactionHasher.Invoke(tx1)), Is.False);
			Assert.That(_BlockChain.pool.ContainsKey(Merkle.transactionHasher.Invoke(tx2)), Is.False);

			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1), Is.True);
			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output2), Is.False);

		}
	}
}
