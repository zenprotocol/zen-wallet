using Consensus;
using BlockChain.Store;
using Store;
using NUnit.Framework;
using BlockChain.Data;

namespace BlockChain.Tests
{
	[TestFixture()]
	public class BlockChainTransactionRelocationTests : BlockChainTestsBase
	{
		private Keyed<Types.Block> block1;
		private Keyed<Types.Block> block2;
		private Keyed<Types.Block> block3;
		private Keyed<Types.Block> block4;
		private Keyed<Types.Block> block5;

		private Keyed<Types.Transaction> block1_tx;
		private Keyed<Types.Transaction> block2_tx;
		private Keyed<Types.Transaction> block3_tx;
		private Keyed<Types.Transaction> block4_tx;
		private Keyed<Types.Transaction> block5_tx;

		[OneTimeSetUp]
		public new void OneTimeSetUp()
		{
			base.OneTimeSetUp();

			block1_tx = GetTx();
			block2_tx = GetTx();
			block3_tx = GetTx();
			block4_tx = GetTx();
			block5_tx = GetTx();

			block1 = GetBlock(_GenesisBlock, block1_tx);
			block2 = GetBlock(_GenesisBlock, block2_tx);
			block3 = GetBlock(block2, block3_tx);
			block4 = GetBlock(block1, block4_tx);
			block5 = GetBlock(block4, block5_tx);
		}

		[Test, Order(1)]
		public void ShouldAddToUtxoSet()
		{
			Assert.That(_BlockChain.HandleNewBlock(_GenesisBlock.Value), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.HandleNewBlock(block1.Value), Is.EqualTo(AddBk.Result.Added));

			AssertUtxoSet(block1_tx.Key, block1_tx.Value, true);
			Assert.That(_BlockChain.HandleNewBlock(block2.Value), Is.EqualTo(AddBk.Result.Added));
			AssertUtxoSet(block2_tx.Key, block2_tx.Value, false); // branch
		}

		[Test, Order(2)]
		public void ShouldReorganizeMempool()
		{
			Assert.That(_BlockChain.HandleNewBlock(block3.Value), Is.EqualTo(AddBk.Result.Added));

			AssertMempool(block1_tx.Key, true);
			AssertUtxoSet(block1_tx.Key, block1_tx.Value, false);

			AssertUtxoSet(block2_tx.Key, block2_tx.Value, true);
			AssertUtxoSet(block3_tx.Key, block3_tx.Value, true);
		}

		[Test, Order(3)]
		public void ShouldNotReorganizeMempoolAgain()
		{
			Assert.That(_BlockChain.HandleNewBlock(block4.Value), Is.EqualTo(AddBk.Result.Added));

			AssertMempool(block1_tx.Key, true);
			AssertUtxoSet(block1_tx.Key, block1_tx.Value, false);

			AssertUtxoSet(block2_tx.Key, block2_tx.Value, true);
			AssertUtxoSet(block3_tx.Key, block3_tx.Value, true);
		}

		[Test, Order(4)]
		public void ShouldReorganizeMempoolAgain()
		{
			Assert.That(_BlockChain.HandleNewBlock(block5.Value), Is.EqualTo(AddBk.Result.Added));

			AssertMempool(block2_tx.Key, true);
			AssertUtxoSet(block2_tx.Key, block2_tx.Value, false);

			AssertMempool(block3_tx.Key, true);
			AssertUtxoSet(block3_tx.Key, block3_tx.Value, false);

			AssertMempool(block1_tx.Key, false);
			AssertUtxoSet(block1_tx.Key, block1_tx.Value, true);

			AssertMempool(block4_tx.Key, false);
			AssertUtxoSet(block4_tx.Key, block4_tx.Value, true);

			AssertMempool(block5_tx.Key, false);
			AssertUtxoSet(block5_tx.Key, block5_tx.Value, true);
		}

		private void AssertMempool(byte[] tx, bool contains)
		{
			Assert.That(_BlockChain.TxMempool.ContainsKey(tx), Is.EqualTo(contains));
		}

		private void AssertUtxoSet(byte[] txHash, Types.Transaction tx, bool contains)
		{
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
