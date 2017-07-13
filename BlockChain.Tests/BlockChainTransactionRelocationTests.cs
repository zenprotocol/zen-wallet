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
        public void Test1_ShouldAddToUtxoSet()
        {
            Assert.That(HandleBlock(_GenesisBlock), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));
            Assert.That(HandleBlock(block1), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));

            Assert.That(CheckUtxsSetContains(block1_tx), Is.EqualTo(true));
            Assert.That(HandleBlock(block2), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));
            Assert.That(CheckUtxsSetContains(block2_tx), Is.EqualTo(false)); // branch
        }

        [Test, Order(2)]
        public void Test2_ShouldReorganizeMempool()
        {
            Assert.That(HandleBlock(block3), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted)); // cause a reorganization

            RegisterTxEvent(block1_tx, TxStateEnum.Unconfirmed);

            Assert.That(_BlockChain.Tip.Value, Is.EqualTo(block3)); // cause a reorganization

            Assert.That(WaitTxState(block1_tx), Is.EqualTo(true), "block1_tx should be made Unconfirmed");

            Assert.That(CheckMempoolContains(block1_tx), Is.EqualTo(true), "block1_tx should be in mempool");
            Assert.That(CheckUtxsSetContains(block1_tx), Is.EqualTo(false), "block1_tx's utxos should be avail");

            Assert.That(CheckUtxsSetContains(block2_tx), Is.EqualTo(true), "block2_tx's utxos' should not be avail");
            Assert.That(CheckUtxsSetContains(block3_tx), Is.EqualTo(true), "block3_tx's utxos' should not be avail");
        }

        [Test, Order(3)]
        public void Test3_ShouldNotReorganizeMempoolAgain()
        {
            Assert.That(HandleBlock(block4), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));

            Assert.That(CheckMempoolContains(block1_tx), Is.EqualTo(true));
            Assert.That(CheckUtxsSetContains(block1_tx), Is.EqualTo(false));

            Assert.That(CheckUtxsSetContains(block2_tx), Is.EqualTo(true));
            Assert.That(CheckUtxsSetContains(block3_tx), Is.EqualTo(true));
        }

        [Test, Order(4)]
        public void Test4_ShouldReorganizeMempoolAgain()
        {
            RegisterTxEvent(block2_tx, TxStateEnum.Unconfirmed);

            Assert.That(HandleBlock(block5), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));

            Assert.That(WaitTxState(block2_tx), Is.EqualTo(true));

            Assert.That(CheckMempoolContains(block2_tx), Is.EqualTo(true));
            Assert.That(CheckUtxsSetContains(block2_tx), Is.EqualTo(false));

            Assert.That(CheckMempoolContains(block3_tx), Is.EqualTo(true));
            Assert.That(CheckUtxsSetContains(block3_tx), Is.EqualTo(false));

            Assert.That(CheckMempoolContains(block1_tx), Is.EqualTo(false));
            Assert.That(CheckUtxsSetContains(block1_tx), Is.EqualTo(true));

            Assert.That(CheckMempoolContains(block4_tx), Is.EqualTo(false));
            Assert.That(CheckUtxsSetContains(block4_tx), Is.EqualTo(true));

            Assert.That(CheckMempoolContains(block5_tx), Is.EqualTo(false));
            Assert.That(CheckUtxsSetContains(block5_tx), Is.EqualTo(true));
        }

        bool CheckMempoolContains(Types.Transaction tx)
        {
             var txHash = Merkle.transactionHasher.Invoke(tx);
            return _BlockChain.memPool.TxPool.Contains(txHash);
        }

        bool CheckUtxsSetContains(Types.Transaction tx)
        {
            var txHash = Merkle.transactionHasher.Invoke(tx);
            var utxos = new GetUTXOSetAction().Publish().Result;

            if (utxos.Item2.ContainsKey(txHash))
            {
                foreach (var output in tx.outputs)
                {
                    if (utxos.Item1[txHash].Contains(output))
                        return true;
                }
            }

            return false;
        }
    }
}
