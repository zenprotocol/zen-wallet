using System;
using Consensus;
using BlockChain.Store;
using Infrastructure;
using System.Collections.Generic;
using System.Linq;
using BlockChain.Data;
using NUnit.Framework;
using System.IO;
using System.Reflection;
using static BlockChain.BlockChain;
using static BlockChain.BlockVerificationHelper;
using System.Threading;

namespace BlockChain
{
    public class BlockChainTestsBase
    {
        const string DB = "temp";
        byte[] _GenesisBlockHash;
        IDisposable _TxMessagesListenerScope;
        HashDictionary<TxStateEnum> _TxStates = new HashDictionary<TxStateEnum>();
        HashDictionary<Tuple<ManualResetEvent, TxStateEnum>> _TxStateEvents = new HashDictionary<Tuple<ManualResetEvent, TxStateEnum>>();
        protected BlockChain _BlockChain;
        protected Types.Block _GenesisBlock;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Dispose();

            _TxMessagesListenerScope = MessageProducer<BlockChainMessage>.Instance.AddMessageListener(
                new MessageListener<BlockChainMessage>(OnBlockChainMessage));

            _GenesisBlock = new GenesisBlock().Block;
            _GenesisBlockHash = Merkle.blockHeaderHasher.Invoke(_GenesisBlock.header);
            _BlockChain = new BlockChain(DB, _GenesisBlockHash);
        }

        [OneTimeTearDown]
        public void Dispose()
        {
            if (_TxMessagesListenerScope != null)
            {
                _TxMessagesListenerScope.Dispose();
            }
            if (_BlockChain != null)
            {
                _BlockChain.Dispose();
            }
            if (Directory.Exists(DB))
            {
                Directory.Delete(DB, true);
            }
        }

        void OnBlockChainMessage(BlockChainMessage m)
        {
            if (m is TxMessage)
            {
                var txMessage = (TxMessage)m;
                _TxStates[txMessage.TxHash] = ((TxMessage)m).State;

                if (_TxStateEvents.ContainsKey(txMessage.TxHash))
                {
                    var expectedTxState = _TxStateEvents[txMessage.TxHash].Item2;

                    if (((TxMessage)m).State == expectedTxState)
                    {
                        _TxStateEvents[txMessage.TxHash].Item1.Set();
                    }
                }
            }
            else if (m is BlockMessage)
            {
                foreach (var item in ((BlockMessage)m).PointedTransactions)
                {
                    _TxStates[item.Key] = TxStateEnum.Confirmed;
                }
            }
        }

        protected LocationEnum Location(Types.Block block)
        {
#if DEBUG
            return new GetBlockLocationAction { Block = Merkle.blockHeaderHasher.Invoke(block.header) }.Publish().Result;
#else
            return LocationEnum.Main;
#endif
        }

			protected TxStateEnum? TxState(Types.Transaction tx)
		{
			var key = Merkle.transactionHasher.Invoke(tx);
			if (_TxStates.ContainsKey(key)) return _TxStates[key];
			return null;
		}

        protected void RegisterTxEvent(Types.Transaction tx, TxStateEnum txState)
        {
            _TxStateEvents[Merkle.transactionHasher.Invoke(tx)] =
	            new Tuple<ManualResetEvent, TxStateEnum>(
	                new ManualResetEvent(false),
	                txState
	            );
		}

        protected bool WaitTxState(Types.Transaction tx)
        {
            return _TxStateEvents[Merkle.transactionHasher.Invoke(tx)].Item1.WaitOne(1500, false);
		}

		protected bool CheckUTXOCOntains(Types.Output output)
		{
			var utxos = new GetUTXOSetAction().Publish().Result;

            foreach (var item in utxos.Item1)
			{
				if (item.Value.Contains(output))
				{
					return true;
				}
			}

			return false;
		}

        protected TxResultEnum HandleTransaction(Types.Transaction tx)
        {
            return new HandleTransactionAction { Tx = tx }.Publish().Result;
        }


        protected BkResultEnum HandleBlock(Types.Block bk)
        {
            return new HandleBlockAction(bk).Publish().Result.BkResultEnum;
        }
	}
}
