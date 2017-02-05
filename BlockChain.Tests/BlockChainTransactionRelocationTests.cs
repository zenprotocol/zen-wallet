using Consensus;
using BlockChain.Store;
using Store;
using NUnit.Framework;
using BlockChain.Data;
using Infrastructure.Testing;

namespace BlockChain.Tests
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

			block1_tx = Utils.GetTx();
			block2_tx = Utils.GetTx();
			block3_tx = Utils.GetTx();
			block4_tx = Utils.GetTx();
			block5_tx = Utils.GetTx();

			block1 = _GenesisBlock.Child().AddTx(block1_tx);
			block2 = _GenesisBlock.Child().AddTx(block2_tx);
			block3 = _GenesisBlock.Child().AddTx(block3_tx);
			block4 = _GenesisBlock.Child().AddTx(block4_tx);
			block5 = _GenesisBlock.Child().AddTx(block5_tx);
		}

		[Test, Order(1)]
		public void ShouldAddToUtxoSet()
		{
			Assert.That(_BlockChain.HandleNewBlock(_GenesisBlock), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.HandleNewBlock(block1), Is.EqualTo(AddBk.Result.Added));

			AssertUtxoSet(block1_tx, true);
			Assert.That(_BlockChain.HandleNewBlock(block2), Is.EqualTo(AddBk.Result.Added));
			AssertUtxoSet(block2_tx, false); // branch
		}

		[Test, Order(2)]
		public void ShouldReorganizeMempool()
		{
			Assert.That(_BlockChain.HandleNewBlock(block3), Is.EqualTo(AddBk.Result.Added)); // cause a reorganization

			AssertMempool(block1_tx, true);
			AssertUtxoSet(block1_tx, false);

			AssertUtxoSet(block2_tx, true);
			AssertUtxoSet(block3_tx, true);
		}

		[Test, Order(3)]
		public void ShouldNotReorganizeMempoolAgain()
		{
			Assert.That(_BlockChain.HandleNewBlock(block4), Is.EqualTo(AddBk.Result.Added));

			AssertMempool(block1_tx, true);
			AssertUtxoSet(block1_tx, false);

			AssertUtxoSet(block2_tx, true);
			AssertUtxoSet(block3_tx, true);
		}

		[Test, Order(4)]
		public void ShouldReorganizeMempoolAgain()
		{
			Assert.That(_BlockChain.HandleNewBlock(block5), Is.EqualTo(AddBk.Result.Added));

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
			Assert.That(_BlockChain.TxMempool.ContainsKey(txHash), Is.EqualTo(contains));
		}

		private void AssertUtxoSet(Types.Transaction tx, bool contains)
		{
			var txHash = Merkle.transactionHasher.Invoke(tx);

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				int i = 0;
				foreach (var output in tx.outputs)
				{
					byte[] outputKey = new byte[txHash.Length + 1];
					txHash.CopyTo(outputKey, 0);
					outputKey[txHash.Length] = (byte)i;

					Assert.That(_BlockChain.UTXOStore.ContainsKey(dbTx, outputKey), Is.EqualTo(contains));

					if (contains)
					{
						Assert.That(_BlockChain.UTXOStore.Get(dbTx, outputKey).Value, Is.EqualTo(output));
					}

					i++;
				}
			}
		}
	}
}
