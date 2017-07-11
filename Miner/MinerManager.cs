using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlockChain;
using BlockChain.Data;
using Consensus;
using Infrastructure;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using System.Linq;
using Miner.Data;
using Wallet.core.Data;

namespace Miner
{
	//TODO: refactor duplication
	using ContractFunction = FSharpFunc<Tuple<byte[], byte[], FSharpFunc<Types.Outpoint, FSharpOption<Types.Output>>>, Tuple<FSharpList<Types.Outpoint>, FSharpList<Types.Output>, byte[]>>;

	public class MinerManager : ResourceOwner
    {
        readonly TransactionQueue _TransactionQueue = new TransactionQueue();
        readonly List<Types.Transaction> _ValidatedTxs = new List<Types.Transaction>();

        HashDictionary<ContractFunction> _ActiveContracts;
        List<KeyValuePair<Types.Outpoint, Types.Output>> _UtxoSet;

		//    readonly HashDictionary<ContractFunc> _ActiveContracts = new HashDictionary<ContractFunc>();
		//readonly IDictionary<Types.Outpoint, Types.Output> _UtxoSet = new Dictionary<Types.Outpoint, Types.Output>();

		BlockChain.BlockChain _BlockChain;
        //TxPool _BlockChainTxPool;
        EventLoopMessageListener<BlockChainMessage> _BlockChainListener;

        //byte[] _Tip;
        //uint _TipBlockNumber;
        readonly Hasher _Hasher = new Hasher();

		public uint Difficulty
		{
			get
			{
				return _Hasher.Difficulty;
			}
			set
			{
				_Hasher.Difficulty = value;
			}
		}

        public event Action<Types.Block> OnMined;

        public int TxsPerBlockLimit { get; set; }

        Types.BlockHeader _Header;
        Types.Transaction _Coinbase;
        Address _Address;

        public MinerManager(BlockChain.BlockChain blockChain, Address address)
        {
            TxsPerBlockLimit = 100; //TODO
            Difficulty = 14;

            _Address = address;
           // _BlockChainTxPool = blockChain.memPool.TxPool;
			_BlockChain = blockChain;
            _Hasher.OnMined += DispatchBlock;
			OwnResource(_Hasher);

			_BlockChainListener = new EventLoopMessageListener<BlockChainMessage>(OnBlockChainMessage, "Miner Consumer");			
			OwnResource(MessageProducer<BlockChainMessage>.Instance.AddMessageListener(_BlockChainListener));

            Reset();
		}

		async void Reset()
		{
			//_Tip = _BlockChain.Tip.Key;
		 	//_TipBlockNumber = _BlockChain.Tip.Value.header.blockNumber;

			_TransactionQueue.Clear();
			_ValidatedTxs.Clear();

			var acsList = await new GetActiveContactsAction().Publish();
			var utxoSet = await new GetUTXOSetAction().Publish();



		//	var minerBlockChainData = await new GetMinerParamsAction().Publish();

			// _ActiveContracts.Clear();

			// do we need that? bk message can update the tip
			//_Tip = minerBlockChainData.Tip;
			//_TipBlockNumber = minerBlockChainData.TipBlockNumber;
			//_ActiveContracts = minerBlockChainData.ActiveContracts;
			//_UtxoSet = minerBlockChainData.UtxoSet;

			Populate();
			RecalculateHeader();
		}

        void Populate()
        {
			_BlockChainListener.Pause();

            _TransactionQueue.Clear();

            lock (_BlockChain.memPool.TxPool)
            {
				foreach (var item in _BlockChain.memPool.TxPool)
                {
                    _TransactionQueue.Push(TransactionValidation.unpoint(item.Value));
                }
            }

            RecalculateHeader();

            _BlockChainListener.Continue();
		}

  //      void Pause()
  //      {
  //          _Hasher.Pause();
  //      }

		//public void Continue()
		//{
		//	_Hasher.Pause();
		//}

		void OnBlockChainMessage(BlockChainMessage m)
		{	
			if (m is TxMessage)
			{
				MinerTrace.Information("Got tx");

				var tx = TransactionValidation.unpoint(((TxMessage)m).Ptx);

                _TransactionQueue.Push(tx);

                RecalculateHeader();
			}
			else if (m is BlockMessage)
			{
				MinerTrace.Information("Got bk");
				
                Reset();
			}	
		}

        bool IsTransactionValid(Types.Transaction tx)
        {
            return true;
        }

        void RecalculateHeader()
        {
			if (_BlockChain.Tip == null)
			{
				return;
			}

			while (!_TransactionQueue.IsStuck && _ValidatedTxs.Count < TxsPerBlockLimit)
			{
				var _tx = _TransactionQueue.Take();

				if (IsTransactionValid(_tx))
				{
					_ValidatedTxs.Add(_tx);

					_TransactionQueue.Remove();
				}
				else
				{
					_TransactionQueue.Next();
				}
			}

            ///////////////////////////////////////////
            /// 
           // if (_TransactionQueue.IsStuck)
          //      _Hasher.Pause("temp: stuck, so - wait for txs");

            CalculateCoinbase();

            _Header = new Types.BlockHeader(
                0,
				_BlockChain.Tip.Key,
				_BlockChain.Tip.Value.header.blockNumber + 1,
                Merkle.merkleRoot(
                    new byte[] { },
                    Merkle.transactionHasher,
                    ListModule.OfSeq(FSharpList<Types.Transaction>.Cons(_Coinbase, ListModule.OfSeq(_ValidatedTxs)))
                ),
				new byte[] { },
				new byte[] { },
                ListModule.Empty<byte[]>(),
                DateTime.Now.ToUniversalTime().Ticks,
                Difficulty,
                new byte[12]
            );

            MinerTrace.Information($"Mining block number {_BlockChain.Tip.Value.header.blockNumber} with {_ValidatedTxs.Count()} txs");

			_Hasher.SetHeader(_Header);
            _Hasher.Continue();
        }

        void CalculateCoinbase()
        {
            var reward = 1000u;

            var outputs = new List<Types.Output>
            {
                new Types.Output(Types.OutputLock.NewPKLock(_Address.Bytes), new Types.Spend(Tests.zhash, reward))
            };

            var witness = new List<byte[]>
            {
                BitConverter.GetBytes(_BlockChain.Tip.Value.header.blockNumber)
            };

            _Coinbase = new Types.Transaction(
                0,
                ListModule.Empty<Types.Outpoint>(),
                ListModule.OfSeq(witness),
                ListModule.OfSeq(outputs),
                FSharpOption<Types.ExtendedContract>.None
            );
        }

        void DispatchBlock()
        {
            MinerTrace.Information("Dispatching block");

            var txs = FSharpList<Types.Transaction>.Cons(_Coinbase, ListModule.OfSeq(_ValidatedTxs));
			var block = new Types.Block(_Header, txs);

			_Hasher.Pause("validating block");

			var result = new HandleBlockAction(block).Publish().Result.BkResultEnum;

			if (result == BlockVerificationHelper.BkResultEnum.Accepted)
			{
				if (OnMined != null)
                	OnMined(block);
            } 
            else
            {
                Reset();
            }

            MinerTrace.Information($"  block {block.header.blockNumber} is " + result);

			//_Hasher.Continue();
		}
	}
}
