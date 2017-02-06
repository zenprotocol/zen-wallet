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

			Assert.That(_BlockChain.HandleNewBlock(_GenesisBlock), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.HandleNewTransaction(txToBeEvictedFromMempool), Is.EqualTo(AddTx.Result.Added));
			Assert.That(_BlockChain.TxMempool.ContainsKey(Merkle.transactionHasher.Invoke(txToBeEvictedFromMempool)), Is.True);

			var txConflicting = Utils.GetTx().AddInput(genesisTx, 0, key.Address).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 1);
			Assert.That(_BlockChain.HandleNewBlock(_GenesisBlock.Child().AddTx(txConflicting)), Is.EqualTo(AddBk.Result.Added));

			Assert.That(_BlockChain.TxMempool.ContainsKey(Merkle.transactionHasher.Invoke(txToBeEvictedFromMempool)), Is.False);
		}

		[Test]
		public void ShouldUndoBlockOnReorganization()
		{
			var key = Key.Create();

			var genesisTx = Utils.GetTx().AddOutput(key.Address, Consensus.Tests.zhash, 50).AddOutput(key.Address, Consensus.Tests.zhash, 50);
			_GenesisBlock = _GenesisBlock.AddTx(genesisTx);
			Assert.That(_BlockChain.HandleNewBlock(_GenesisBlock), Is.EqualTo(AddBk.Result.Added));

			var output1_withconflict = Utils.GetOutput(Key.Create().Address, Consensus.Tests.zhash, 5);
			var tx1_withconflict = Utils.GetTx().AddInput(genesisTx, 0, key.Address).AddOutput(output1_withconflict);
			var output1_withoutconflict = Utils.GetOutput(Key.Create().Address, Consensus.Tests.zhash, 5);
			var tx1_withoutconflict = Utils.GetTx().AddInput(genesisTx, 1, key.Address).AddOutput(output1_withoutconflict);
			Assert.That(_BlockChain.HandleNewBlock(_GenesisBlock.Child().AddTx(tx1_withconflict).AddTx(tx1_withoutconflict)), Is.EqualTo(AddBk.Result.Added));

			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1_withconflict), Is.True);
			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1_withoutconflict), Is.True);

			var output2 = Utils.GetOutput(Key.Create().Address, Consensus.Tests.zhash, 5);
			var tx2_withconflict = Utils.GetTx().AddInput(genesisTx, 0, key.Address).AddOutput(output2);
			var sideChainBlock = _GenesisBlock.Child().AddTx(tx2_withconflict);
			Assert.That(_BlockChain.HandleNewBlock(sideChainBlock), Is.EqualTo(AddBk.Result.Added));

			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1_withconflict), Is.True);
			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1_withoutconflict), Is.True);
			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output2), Is.False);

			Assert.That(_BlockChain.HandleNewBlock(sideChainBlock.Child()), Is.EqualTo(AddBk.Result.Added));

			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1_withconflict), Is.False);
			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1_withoutconflict), Is.False);
			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output2), Is.True);

			Assert.That(_BlockChain.TxMempool.ContainsKey(Merkle.transactionHasher.Invoke(tx1_withconflict)), Is.False);
			Assert.That(_BlockChain.TxMempool.ContainsKey(Merkle.transactionHasher.Invoke(tx1_withoutconflict)), Is.True);
		}

		[Test]
		public void ShouldNotPutInvalidatedTxIntoMempoolWhenReorganizing()
		{
			var key = Key.Create();

			var genesisTx = Utils.GetTx().AddOutput(key.Address, Consensus.Tests.zhash, 100);
			_GenesisBlock = _GenesisBlock.AddTx(genesisTx);

			var txSpending1 = Utils.GetTx().AddInput(genesisTx, 0, key.Address).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 1);
			var txSpending2 = Utils.GetTx().AddInput(genesisTx, 0, key.Address).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 1);

			Assert.That(_BlockChain.HandleNewBlock(_GenesisBlock), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.HandleNewBlock(_GenesisBlock.Child().AddTx(txSpending1)), Is.EqualTo(AddBk.Result.Added));

			var sideChainBlock = _GenesisBlock.Child().AddTx(txSpending2);
			Assert.That(_BlockChain.HandleNewBlock(sideChainBlock), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.HandleNewBlock(sideChainBlock.Child()), Is.EqualTo(AddBk.Result.Added)); // reorganize

			Assert.That(_BlockChain.TxMempool.ContainsKey(Merkle.transactionHasher.Invoke(txSpending1)), Is.False);
			Assert.That(_BlockChain.TxMempool.ContainsKey(Merkle.transactionHasher.Invoke(txSpending2)), Is.False);
		}

		[Test]
		public void ShouldUndoBlockOnReorganizationWithOrphanBlock()
		{
			var key = Key.Create();

			var genesisTx = Utils.GetTx().AddOutput(key.Address, Consensus.Tests.zhash, 50).AddOutput(key.Address, Consensus.Tests.zhash, 50);
			_GenesisBlock = _GenesisBlock.AddTx(genesisTx);
			Assert.That(_BlockChain.HandleNewBlock(_GenesisBlock), Is.EqualTo(AddBk.Result.Added));

			var output1_withconflict = Utils.GetOutput(Key.Create().Address, Consensus.Tests.zhash, 5);
			var tx1_withconflict = Utils.GetTx().AddInput(genesisTx, 0, key.Address).AddOutput(output1_withconflict);
			var output1_withoutconflict = Utils.GetOutput(Key.Create().Address, Consensus.Tests.zhash, 5);
			var tx1_withoutconflict = Utils.GetTx().AddInput(genesisTx, 1, key.Address).AddOutput(output1_withoutconflict);
			Assert.That(_BlockChain.HandleNewBlock(_GenesisBlock.Child().AddTx(tx1_withconflict).AddTx(tx1_withoutconflict)), Is.EqualTo(AddBk.Result.Added));

			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1_withconflict), Is.True);
			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1_withoutconflict), Is.True);

			var output2 = Utils.GetOutput(Key.Create().Address, Consensus.Tests.zhash, 5);
			var tx2_withconflict = Utils.GetTx().AddInput(genesisTx, 0, key.Address).AddOutput(output2);
			var sideChainBlock = _GenesisBlock.Child().AddTx(tx2_withconflict);


			Assert.That(_BlockChain.HandleNewBlock(sideChainBlock.Child()), Is.EqualTo(AddBk.Result.AddedOrphan));

			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1_withconflict), Is.True);
			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1_withoutconflict), Is.True);
			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output2), Is.False);

			Assert.That(_BlockChain.HandleNewBlock(sideChainBlock), Is.EqualTo(AddBk.Result.Added));

			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1_withconflict), Is.False);
			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1_withoutconflict), Is.False);
			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output2), Is.True);

			Assert.That(_BlockChain.TxMempool.ContainsKey(Merkle.transactionHasher.Invoke(tx1_withconflict)), Is.False);
			Assert.That(_BlockChain.TxMempool.ContainsKey(Merkle.transactionHasher.Invoke(tx1_withoutconflict)), Is.True);
		}

		[Test]
		public void ShouldUndoReorganization()
		{ 
			var key = Key.Create();

			var genesisTx = Utils.GetTx().AddOutput(key.Address, Consensus.Tests.zhash, 100);
			_GenesisBlock = _GenesisBlock.AddTx(genesisTx);

			var output1 = Utils.GetOutput(Key.Create().Address, Consensus.Tests.zhash, 5);
			var output2 = Utils.GetOutput(Key.Create().Address, Consensus.Tests.zhash, 5);

			var txSpending1 = Utils.GetTx().AddInput(genesisTx, 0, key.Address).AddOutput(output1);
			var txSpending2 = Utils.GetTx().AddInput(genesisTx, 0, key.Address).AddOutput(output2);

			Assert.That(_BlockChain.HandleNewBlock(_GenesisBlock), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.HandleNewBlock(_GenesisBlock.Child().AddTx(txSpending1)), Is.EqualTo(AddBk.Result.Added));

			var sideChainBlock = _GenesisBlock.Child().AddTx(txSpending2);
			Assert.That(_BlockChain.HandleNewBlock(sideChainBlock), Is.EqualTo(AddBk.Result.Added));

			Assert.That(_BlockChain.HandleNewBlock(sideChainBlock.Child().AddTx(genesisTx)), Is.EqualTo(AddBk.Result.Rejected)); // should undo reorganization

			Assert.That(_BlockChain.TxMempool.ContainsKey(Merkle.transactionHasher.Invoke(txSpending1)), Is.False);
			Assert.That(_BlockChain.TxMempool.ContainsKey(Merkle.transactionHasher.Invoke(txSpending2)), Is.False);

			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output1), Is.True);
			Assert.That(_BlockChain.GetUTXOSet(null).Values.Contains(output2), Is.False);

		}
	}
}
