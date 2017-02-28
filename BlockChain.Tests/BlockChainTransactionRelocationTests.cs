using Consensus;
using BlockChain.Store;
using Store;
using NUnit.Framework;
using BlockChain.Data;
using Wallet.core.Data;

namespace BlockChain
{
	[TestFixture()]
	public class BlockChainTransactionRelocationTests : BlockChainTestsBase
	{
		private Types.Block block1;
		private Types.Block block2;
		private Types.Block block3;
		private Types.Block block4;
		private Types.Block block5;

		private Types.Transaction block1_tx;
		private Types.Transaction block2_tx;
		private Types.Transaction block3_tx;
		private Types.Transaction block4_tx;
		private Types.Transaction block5_tx;

		[OneTimeSetUp]
		public new void OneTimeSetUp()
		{
			base.OneTimeSetUp();

			block1_tx = Utils.GetTx().AddOutput(Key.Create().Address, Consensus.Tests.zhash, 1).Tag("block1_tx");
			block2_tx = Utils.GetTx().AddOutput(Key.Create().Address, Consensus.Tests.zhash, 1).Tag("block2_tx");
			block3_tx = Utils.GetTx().AddOutput(Key.Create().Address, Consensus.Tests.zhash, 1).Tag("block3_tx");
			block4_tx = Utils.GetTx().AddOutput(Key.Create().Address, Consensus.Tests.zhash, 1).Tag("block4_tx");
			block5_tx = Utils.GetTx().AddOutput(Key.Create().Address, Consensus.Tests.zhash, 1).Tag("block5_tx");

			block1 = _GenesisBlock.Child().AddTx(block1_tx).Tag("block1");
			block2 = _GenesisBlock.Child().AddTx(block2_tx).Tag("block2");
			block3 = block2.Child().AddTx(block3_tx).Tag("block3");
			block4 = block1.Child().AddTx(block4_tx).Tag("block4");
			block5 = block4.Child().AddTx(block5_tx).Tag("block5");
		}

		[Test, Order(1)]
		public void ShouldAddToUtxoSet()
		{
			Assert.That(_BlockChain.HandleBlock(_GenesisBlock), Is.True);
			Assert.That(_BlockChain.HandleBlock(block1), Is.True);

			AssertUtxoSet(block1_tx, true);
			Assert.That(_BlockChain.HandleBlock(block2), Is.True);
			AssertUtxoSet(block2_tx, false); // branch
		}

		[Test, Order(2)]
		public void ShouldReorganizeMempool()
		{
			Assert.That(_BlockChain.HandleBlock(block3), Is.True); // cause a reorganization
			Assert.That(_BlockChain.Tip.Value, Is.EqualTo(block3)); // cause a reorganization

			AssertMempool(block1_tx, true);
			AssertUtxoSet(block1_tx, false);

			AssertUtxoSet(block2_tx, true);
			AssertUtxoSet(block3_tx, true);
		}

		[Test, Order(3)]
		public void ShouldNotReorganizeMempoolAgain()
		{
			Assert.That(_BlockChain.HandleBlock(block4), Is.True);

			AssertMempool(block1_tx, true);
			AssertUtxoSet(block1_tx, false);

			AssertUtxoSet(block2_tx, true);
			AssertUtxoSet(block3_tx, true);
		}

		[Test, Order(4)]
		public void ShouldReorganizeMempoolAgain()
		{
			Assert.That(_BlockChain.HandleBlock(block5), Is.True);

			AssertMempool(block2_tx, true);
			AssertUtxoSet(block2_tx, false);

			AssertMempool(block3_tx, true);
			AssertUtxoSet(block3_tx, false);

			AssertMempool(block1_tx, false);
			AssertUtxoSet(block1_tx, true);

			AssertMempool(block4_tx, false);
			AssertUtxoSet(block4_tx, true);

			AssertMempool(block5_tx, false);
			AssertUtxoSet(block5_tx, true);
		}

		private void AssertMempool(Types.Transaction tx, bool contains)
		{
		 	var txHash = Merkle.transactionHasher.Invoke(tx);
			Assert.That(_BlockChain.memPool.TxPool.Contains(txHash), Is.EqualTo(contains));
		}

		private void AssertUtxoSet(Types.Transaction tx, bool contains)
		{
			var txHash = Merkle.transactionHasher.Invoke(tx);

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				uint i = 0;
				foreach (var output in tx.outputs)
				{
					Assert.That(_BlockChain.UTXOStore.ContainsKey(dbTx, txHash, i), Is.EqualTo(contains));

					if (contains)
					{
						Assert.That(_BlockChain.UTXOStore.Get(dbTx, txHash, i).Value, Is.EqualTo(output));
					}

					i++;
				}
			}
		}
	}
}
