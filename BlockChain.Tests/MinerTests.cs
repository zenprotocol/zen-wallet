using System;
using System.Threading;
using Miner;
using NUnit.Framework;
using Wallet.core.Data;
using static BlockChain.BlockChain;
using static BlockChain.BlockVerificationHelper;

namespace BlockChain
{
    public class MinerTests : BlockChainTestsBase
    {
        [Test()]
        //TODO: ensure txs are in the right order inside the miner
        public void ShouldOrderTxsInBlock()
        {
            var myKey = Key.Create();
            var me = myKey.Address;
            var them = Key.Create().Address;

            var genesisTx = Utils.GetTx().AddOutput(me, Consensus.Tests.zhash, 1000);
            _GenesisBlock = _GenesisBlock.AddTx(genesisTx);
            Assert.That(HandleBlock(_GenesisBlock), Is.EqualTo(BkResultEnum.Accepted));

            var fst = Utils.GetTx()
                           .AddInput(genesisTx, 0)
                           .AddOutput(them, Consensus.Tests.zhash, 10)
                           .AddOutput(me, Consensus.Tests.zhash, 990)
                           .Sign(new byte[][] { myKey.Private });
            Assert.That(HandleTransaction(fst), Is.EqualTo(TxResultEnum.Accepted));

            // snd depends on fst - expecting test to try validated it first and fail, then reverse order and succeed
            var snd = Utils.GetTx()
                           .AddInput(fst, 0)
                           .AddOutput(them, Consensus.Tests.zhash, 20)
                           .AddOutput(me, Consensus.Tests.zhash, 970)
                           .Sign(new byte[][] { myKey.Private });
            Assert.That(HandleTransaction(snd), Is.EqualTo(TxResultEnum.Accepted));

            var evt = new ManualResetEvent(false);

            var miner = new MinerManager(_BlockChain, me);

            Consensus.Types.Block minedBlock = null;

            miner.OnMined += bk =>
            {
                minedBlock = bk;
                evt.Set();
            };

            Assert.That(evt.WaitOne(5000), Is.True);
            Assert.That(minedBlock.transactions, Is.Not.Null);
			Assert.That(minedBlock.transactions.Length, Is.EqualTo(3)); //2 + 1 coinbase
		}
    }
}
