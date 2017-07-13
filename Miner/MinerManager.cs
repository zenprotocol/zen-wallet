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
	using UtxoLookup = FSharpFunc<Types.Outpoint, FSharpOption<Types.Output>>;

	public class MinerManager : ResourceOwner
    {
        readonly TransactionQueue _TransactionQueue = new TransactionQueue();
        readonly List<TransactionValidation.PointedTransaction> _ValidatedTxs = new List<TransactionValidation.PointedTransaction>();

		readonly HashDictionary<ContractFunction> _ActiveContracts = new HashDictionary<ContractFunction>();
		List<Tuple<Types.Outpoint, Types.Output>> _UtxoSet;

		BlockChain.BlockChain _BlockChain;
        EventLoopMessageListener<BlockChainMessage> _BlockChainListener;

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
			_BlockChain = blockChain;
            _Hasher.OnMined += DispatchBlock;
			OwnResource(_Hasher);

			_BlockChainListener = new EventLoopMessageListener<BlockChainMessage>(OnBlockChainMessage, "Miner Consumer");			
			OwnResource(MessageProducer<BlockChainMessage>.Instance.AddMessageListener(_BlockChainListener));

            Reset();
		}

        async void Reset()
        {
            MinerTrace.Information("Miner reset");

            _TransactionQueue.Clear();
            _ValidatedTxs.Clear();

            var acsList = await new GetActiveContactsAction().Publish();
            _UtxoSet = await new GetUTXOSetAction2().Publish();

			_ActiveContracts.Clear();

            acsList.ForEach(t =>
            {
                //TODO: cache compiled?
                var contractFunction = ContractExamples.Execution.deserialize(t.CompiledContract);

                _ActiveContracts[t.Hash] = contractFunction;
            });

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
                    _TransactionQueue.Push(item.Value);
                }
            }

            RecalculateHeader();

            _BlockChainListener.Continue();
		}

		void OnBlockChainMessage(BlockChainMessage m)
		{	
			if (m is TxMessage)
			{
				MinerTrace.Information("Got tx");

                if (((TxMessage)m).State == TxStateEnum.Unconfirmed)
                    _TransactionQueue.Push(((TxMessage)m).Ptx);

                RecalculateHeader();
			}
			else if (m is BlockMessage)
			{
				MinerTrace.Information("Got bk");
				
                Reset();
			}	
		}

        bool IsTransactionValid(TransactionValidation.PointedTransaction ptx)
        {
			UtxoLookup utxoLookup = UtxoLookup.FromConverter(outpoint =>
			{
                var outputs = _UtxoSet.Where(t => t.Item1.Equals(outpoint)).Select(t => t.Item2);
                return !outputs.Any() ? FSharpOption<Types.Output>.None : new FSharpOption<Types.Output>(outputs.First());
            });

            FSharpFunc<byte[], FSharpOption<ContractFunction>> contractLookup =
                FSharpFunc<byte[], FSharpOption<ContractFunction>>.FromConverter(contractHash =>
            {
                return !_ActiveContracts.ContainsKey(contractHash) ? 
                       FSharpOption<ContractFunction>.None :
                       new FSharpOption<ContractFunction>(_ActiveContracts[contractHash]);
            });

            var result = TransactionValidation.validateNonCoinbaseTx(
                ptx,
                utxoLookup,
                contractLookup
            );

			if (!result)
			{
				MinerTrace.Information("Tx invalid");
			}

			return result;
        }

		void HandleTx(TransactionValidation.PointedTransaction ptx)
		{
            foreach (var pInput in ptx.pInputs)
            {
                var toremove = _UtxoSet.Where(t => t.Item1.Equals(pInput.Item1));

                if (toremove.Any())
                {
                    toremove.ToList().ForEach(t => _UtxoSet.Remove(t));
                }
			}

            //TODO: try simplify using hash from message
            var txHash = Merkle.transactionHasher.Invoke(TransactionValidation.unpoint(ptx));
			
            var activationSacrifice = 0UL;

            for (var i = 0; i < ptx.outputs.Length; i++)
            {
				var output = ptx.outputs[i];

				if (output.@lock.IsContractSacrificeLock)
				{
					if (!output.spend.asset.SequenceEqual(Tests.zhash))
						continue; // not Zen

					var contractSacrificeLock = (Types.OutputLock.ContractSacrificeLock)output.@lock;

					if (contractSacrificeLock.IsHighVLock)
						continue; // not current version

                    if (contractSacrificeLock.Item.lockData.Length == 0)
					{
						activationSacrifice += output.spend.amount;
					}
				}

				//todo: fix  to exclude CSLocks&FLocks, instead of including by locktype
				if (output.@lock.IsPKLock || output.@lock.IsContractLock)
				{
					var outpoint = new Types.Outpoint(txHash, (uint)i);
					_UtxoSet.Add(new Tuple<Types.Outpoint, Types.Output>(outpoint, output));
				}
			}

			if (FSharpOption<Types.ExtendedContract>.get_IsSome(ptx.contract) && !ptx.contract.Value.IsHighVContract)
			{
				var codeBytes = ((Types.ExtendedContract.Contract)ptx.contract.Value).Item.code;
				var contractHash = Merkle.innerHash(codeBytes);
				var contractCode = System.Text.Encoding.ASCII.GetString(codeBytes);

                if (!_ActiveContracts.ContainsKey(contractHash))
                {
                    if (activationSacrifice > ActiveContractSet.KalapasPerBlock(contractCode))
                    {
						try
						{
							var compiledCodeOpt = ContractExamples.Execution.compile(contractCode);

                            if (FSharpOption<byte[]>.get_IsSome(compiledCodeOpt))
                            {
                                _ActiveContracts[contractHash] = ContractExamples.Execution.deserialize(compiledCodeOpt.Value);
                            }
						}
						catch (Exception e)
						{
                            MinerTrace.Information("Error compiling contract");
						}
					}
                }
			}
		}

		void RecalculateHeader()
        {
			if (_BlockChain.Tip == null)
			{
				return;
			}

			while (!_TransactionQueue.IsStuck && _ValidatedTxs.Count < TxsPerBlockLimit)
			{
				var ptx = _TransactionQueue.Take();

				if (IsTransactionValid(ptx))
				{
					_ValidatedTxs.Add(ptx);

					_TransactionQueue.Remove();

                    HandleTx(ptx);
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

            var txs = ListModule.OfSeq(FSharpList<Types.Transaction>.Cons(_Coinbase, ListModule.OfSeq(_ValidatedTxs.Select(TransactionValidation.unpoint))));

            _Header = new Types.BlockHeader(
                0,
				_BlockChain.Tip.Key,
				_BlockChain.Tip.Value.header.blockNumber + 1,
                Merkle.merkleRoot(
                    new byte[] { },
                    Merkle.transactionHasher,
                    txs
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
                BitConverter.GetBytes(_BlockChain.Tip.Value.header.blockNumber + 1)
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

			var txs = FSharpList<Types.Transaction>.Cons(_Coinbase, ListModule.OfSeq(_ValidatedTxs.Select(TransactionValidation.unpoint)));
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
