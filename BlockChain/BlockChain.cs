using System;
using Consensus;
using BlockChain.Store;
using Store;
using Infrastructure;
using System.Collections.Generic;
using Microsoft.FSharp.Collections;
using System.Linq;
using BlockChain.Data;
using System.Threading.Tasks.Dataflow;
using System.Threading.Tasks;
using System.Collections;
using Microsoft.FSharp.Core;
using System.Text;
using static BlockChain.BlockVerificationHelper;

namespace BlockChain
{
	public class ContractArgs
	{
		public byte[] ContractHash { get; set; }
		public Func<Types.Outpoint, FSharpOption<Types.Output>> tryFindUTXOFunc { get; set; }
		public byte[] Message { get; set; }
	}

	public class BlockChain : ResourceOwner
	{
#if TEST
		const int COINBASE_MATURITY = 0;
#else
		const int COINBASE_MATURITY = 100;
#endif

		readonly TimeSpan OLD_TIP_TIME_SPAN = TimeSpan.FromMinutes(5);

		DBContext _DBContext = null;

		public MemPool memPool { get; set; }
		public UTXOStore UTXOStore { get; set; }
		public BlockStore BlockStore { get; set; }
		public BlockNumberDifficulties BlockNumberDifficulties { get; set; }
		public ChainTip ChainTip { get; set; }
		public BlockTimestamps Timestamps { get; set; }
		public byte[] GenesisBlockHash { get; set; }

		public enum TxResultEnum
		{
			Accepted,
			OrphanMissingInputs, // don't ban peer
			OrphanIC, // don't ban peer
			Invalid, // ban peer or inform wallet
			DoubleSpend, //don't ban peer
			Known
		}

		public enum IsTxOrphanResult
		{
			NotOrphan,
			Orphan,
			Invalid,
		}

		public enum IsContractGeneratedTxResult
		{
			NotContractGenerated,
			ContractGenerated,
			Invalid,
		}

		public BlockChain(string dbName, byte[] genesisBlockHash)
		{
			memPool = new MemPool();
			UTXOStore = new UTXOStore();
			BlockStore = new BlockStore();
			BlockNumberDifficulties = new BlockNumberDifficulties();
			ChainTip = new ChainTip();
			Timestamps = new BlockTimestamps();
			GenesisBlockHash = genesisBlockHash;

			_DBContext = new DBContext(dbName);
			OwnResource(_DBContext);

			using (var context = _DBContext.GetTransactionContext())
			{
				var chainTip = ChainTip.Context(context).Value;

				//TODO: check if makred as main?
				Tip = chainTip == null ? null : BlockStore.GetBlock(context, chainTip);

				InitBlockTimestamps(context);
			}

			var listener = new EventLoopMessageListener<QueueAction>(HandleQueueAction, "BlockChain listener");

			MessageProducer<QueueAction>.Instance.AddMessageListener(listener);

			OwnResource(listener);
		}

		void HandleQueueAction(QueueAction action)
		{
			try
			{
				//((dynamic)this).Handle((dynamic)action);

				if (action is HandleBlockAction)
					((HandleBlockAction)action).SetResult(HandleBlock(action as HandleBlockAction));
				else if (action is GetActiveContactsAction)
					((GetActiveContactsAction)action).SetResult(GetActiveContacts());
				else if (action is GetContractPointedOutputsAction)
					((GetContractPointedOutputsAction)action).SetResult(GetContractPointedOutputs(
						((GetContractPointedOutputsAction)action).ContractHash));
				else if (action is HandleOrphansOfTxAction)
					HandleOrphansOfTransaction(action as HandleOrphansOfTxAction);
				else if (action is GetIsContractActiveAction)
					((GetIsContractActiveAction)action).SetResult(IsContractActive(
						((GetIsContractActiveAction)action).ContractHash));
				else if (action is GetUTXOAction)
					((GetUTXOAction)action).SetResult(GetUTXO(((GetUTXOAction)action).Outpoint, ((GetUTXOAction)action).IsInBlock));
				else if (action is GetIsConfirmedUTXOExistAction)
				{
					var outpoint = ((GetIsConfirmedUTXOExistAction)action).Outpoint;
					((GetIsConfirmedUTXOExistAction)action).SetResult(IsConfirmedUTXOExist(outpoint));
				}
				else if (action is GetContractCodeAction)
					((GetContractCodeAction)action).SetResult(GetContractCode(
						((GetContractCodeAction)action).ContractHash));
				else if (action is HandleTransactionAction)
					((HandleTransactionAction)action).SetResult(HandleTransaction(((HandleTransactionAction)action).Tx));
				else if (action is GetBlockAction)
					((GetBlockAction)action).SetResult(GetBlock(((GetBlockAction)action).BkHash));
				else if (action is GetTxAction)
					((GetTxAction)action).SetResult(GetTransaction(((GetTxAction)action).TxHash));
				else if (action is ExecuteContractAction)
				{
					var executeContractAction = ((ExecuteContractAction)action);
					Types.Transaction tx;
					var result = ExecuteContract(executeContractAction.ContractHash, executeContractAction.Message, out tx, executeContractAction.Message != null, false);
					((ExecuteContractAction)action).SetResult(new Tuple<bool, Types.Transaction>(result, tx));
				}
				else if (action is GetUTXOSetAction)
				{
					var getUTXOSetAction = (GetUTXOSetAction)action;
					HashDictionary<List<Types.Output>> txOutputs;
					HashDictionary<Types.Transaction> txs;
					GetUTXOSet(getUTXOSetAction.Predicate, out txOutputs, out txs);
					getUTXOSetAction.SetResult(new Tuple<HashDictionary<List<Types.Output>>, HashDictionary<Types.Transaction>>(txOutputs, txs));
				}
			}
			catch (Exception e)
			{
				BlockChainTrace.Error("BlockChain handler", e);
			}
		}

		void HandleOrphansOfTransaction(HandleOrphansOfTxAction a)
		{
			using (var dbTx = _DBContext.GetTransactionContext())
			{
				lock (memPool)
				{
					memPool.OrphanTxPool.GetOrphansOf(a.TxHash).ToList().ForEach(t =>
					{
						TransactionValidation.PointedTransaction ptx;
						var tx = memPool.OrphanTxPool[t];

						switch (IsOrphanTx(dbTx, tx, out ptx))
						{
							case IsTxOrphanResult.Orphan:
								return;
							case IsTxOrphanResult.Invalid:
								BlockChainTrace.Information("invalid orphan tx removed from orphans", tx);
								break;
							case IsTxOrphanResult.NotOrphan:
								if (!memPool.TxPool.ContainsInputs(tx))
								{
									if (IsValidTransaction(dbTx, ptx))
									{
										BlockChainTrace.Information("unorphaned tx added to mempool", tx);
										memPool.TxPool.Add(t, ptx);
									}
									else
									{
										BlockChainTrace.Information("invalid orphan tx removed from orphans", tx);
									}
								}
								else
								{
									BlockChainTrace.Information("double spent orphan tx removed from orphans", tx);
								}
								break;
						}

						memPool.OrphanTxPool.RemoveDependencies(t);
					});
				}
			}
		}

		/// <summary>
		/// Handles a new transaction from network or wallet. 
		/// </summary>
		TxResultEnum HandleTransaction(Types.Transaction tx)
		{
			using (var dbTx = _DBContext.GetTransactionContext())
			{
				TransactionValidation.PointedTransaction ptx;
				var txHash = Merkle.transactionHasher.Invoke(tx);

				lock (memPool)
				{
					if (memPool.TxPool.Contains(txHash))
					{
						BlockChainTrace.Information("Tx already in mempool", txHash);
						return TxResultEnum.Known;
					}

					if (BlockStore.TxStore.ContainsKey(dbTx, txHash))
					{
						BlockChainTrace.Information("Tx already in store", txHash);
						return TxResultEnum.Known;
					}

					if (memPool.TxPool.ContainsInputs(tx))
					{
						BlockChainTrace.Information("Mempool contains spending input", tx);
						return TxResultEnum.DoubleSpend;
					}

					switch (IsOrphanTx(dbTx, tx, out ptx))
					{
						case IsTxOrphanResult.Orphan:
							BlockChainTrace.Information("tx added as orphan", tx);
							memPool.OrphanTxPool.Add(txHash, tx);
							return TxResultEnum.OrphanMissingInputs;
						case IsTxOrphanResult.Invalid:
							BlockChainTrace.Information("tx contains invalid reference(s)", tx);
							return TxResultEnum.Invalid;
					}

					if (!IsCoinbaseTxsValid(dbTx, ptx))
					{
						BlockChainTrace.Information("referenced coinbase immature", tx);
						return TxResultEnum.Invalid;
					}

					byte[] contractHash;
					switch (IsContractGeneratedTx(ptx, out contractHash))
					{
						case IsContractGeneratedTxResult.NotContractGenerated:
							if (!IsValidTransaction(dbTx, ptx))
							{
								BlockChainTrace.Information("tx invalid - universal", ptx);
								return TxResultEnum.Invalid;
							}
							break;
						case IsContractGeneratedTxResult.ContractGenerated:
							if (!new ActiveContractSet().IsActive(dbTx, contractHash))
							{
								BlockChainTrace.Information("tx added to ICTx mempool", tx);
								BlockChainTrace.Information(" of contract", contractHash);
								memPool.TxPool.ICTxPool.Add(txHash, ptx);
								return TxResultEnum.OrphanIC;
							}
							if (!IsValidTransaction(dbTx, ptx))
							{
								BlockChainTrace.Information("tx invalid - universal", ptx);
								return TxResultEnum.Invalid;
							}
							if (!IsValidTransaction(dbTx, ptx, contractHash))
							{
								BlockChainTrace.Information("tx invalid - invalid contract", ptx);
								return TxResultEnum.Invalid;
							}
							break;
						case IsContractGeneratedTxResult.Invalid:
							BlockChainTrace.Information("tx invalid - input locks", tx);
							return TxResultEnum.Invalid;
					}

					BlockChainTrace.Information("tx added to mempool", ptx);
					memPool.TxPool.Add(txHash, ptx);
				}
				return TxResultEnum.Accepted;
			}
		}

		bool IsCoinbaseTxsValid(TransactionContext dbTx, TransactionValidation.PointedTransaction ptx)
		{
			var currentHeight = Tip == null ? 0 : Tip.Value.header.blockNumber;

			foreach (var refTx in ptx.pInputs.Select(t => t.Item1.txHash))
			{
				Types.BlockHeader refTxBk;
				if (BlockStore.IsCoinbaseTx(dbTx, refTx, out refTxBk))
				{
					if (refTxBk.blockNumber - currentHeight < COINBASE_MATURITY)
					{
						return false;
					}
				}
			}

			return true;
		}

		public Task<BkResult> HandleBlock(Types.Block bk)
		{
			return new HandleBlockAction(bk).Publish();
		}

		BlockVerificationHelper.BkResult HandleBlock(HandleBlockAction a)
		{
			BlockVerificationHelper action = null;
            var orphans = new List<Keyed<Types.Block>>();

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				action = new BlockVerificationHelper(
					this,
					dbTx,
					a.BkHash,
					a.Bk,
					a.IsOrphan
				);

				switch (action.Result.BkResultEnum)
				{
					case BlockVerificationHelper.BkResultEnum.AcceptedOrphan:
						dbTx.Commit();
						break;
					case BlockVerificationHelper.BkResultEnum.Accepted:
						UpdateMempool(dbTx, action.ConfirmedTxs, action.UnconfirmedTxs);
						break;
					case BlockVerificationHelper.BkResultEnum.Rejected:
					case BlockVerificationHelper.BkResultEnum.KnownOrphan:
						return action.Result;
				}

                orphans = BlockStore.Orphans(dbTx, a.BkHash).ToList();
			}

            orphans.ForEach(t => new HandleBlockAction(t.Key, t.Value, true).Publish());

			action.QueuedActions.ForEach(t =>
			{
				if (t is MessageAction)
					(t as MessageAction).Message.Publish();
				else
					a.Publish();
			});

			return action.Result;
		}

		void UpdateMempool(TransactionContext dbTx, HashDictionary<TransactionValidation.PointedTransaction> confirmedTxs, HashDictionary<Types.Transaction> unconfirmedTxs)
		{
			lock (memPool)
			{
				dbTx.Commit();

				var activeContracts = new ActiveContractSet().Keys(dbTx);
				activeContracts.AddRange(memPool.ContractPool.Keys);

				EvictTxToMempool(dbTx, unconfirmedTxs);
				RemoveConfirmedTxFromMempool(confirmedTxs);

				PurgeICTxPool(activeContracts, dbTx, Tip.Value.header);
				memPool.TxPool.MoveToICTxPool(activeContracts);
			}
		}

		public void PurgeICTxPool(HashSet activeContracts, TransactionContext dbTx, Types.BlockHeader blockHeader)
		{
			foreach (var item in memPool.ICTxPool.ToList())
			{
				var contractHash = ((Types.OutputLock.ContractLock)item.Value.pInputs.Head.Item2.@lock).contractHash;

				if (activeContracts.Contains(contractHash) && IsValidTransaction(dbTx, item.Value, contractHash))
				{
					memPool.ICTxPool.Remove(item.Key);
					memPool.TxPool.Add(item.Key, item.Value);
					new TxMessage(item.Key, item.Value, TxStateEnum.Unconfirmed).Publish();
					new HandleOrphansOfTxAction(item.Key).Publish();
					// todo check if ptx **activates a contract** and update contractpool if it does
				}
			}
		}

		void EvictTxToMempool(TransactionContext dbTx, HashDictionary<Types.Transaction> unconfirmedTxs)
		{
			foreach (var tx in unconfirmedTxs)
			{
				TransactionValidation.PointedTransaction ptx = null;

				switch (IsOrphanTx(dbTx, tx.Value, out ptx))
				{
					case IsTxOrphanResult.NotOrphan:
						//if (!memPool.TxPool.ContainsInputs(TransactionValidation.unpoint(ptx)))
						//{
						BlockChainTrace.Information("tx evicted to mempool", ptx);
						memPool.TxPool.Add(tx.Key, ptx);
						new TxMessage(tx.Key, ptx, TxStateEnum.Unconfirmed).Publish();
						//}
						//else
						//{
						//	BlockChainTrace.Information("double spent tx not evicted to mempool", ptx);
						//	new TxMessage(tx.Key, ptx, TxStateEnum.Invalid).Publish();
						//}
						break;
					case IsTxOrphanResult.Orphan: // is a double-spend
						BlockChainTrace.Information("double spent tx not evicted to mempool", tx.Key);
						memPool.TxPool.RemoveDependencies(tx.Key);
						new TxMessage(tx.Key, null, TxStateEnum.Invalid).Publish();
						break;
				}
			}
		}

		void RemoveConfirmedTxFromMempool(HashDictionary<TransactionValidation.PointedTransaction> confirmedTxs)
		{
			var spentOutputs = new List<Types.Outpoint>(); //TODO sort - efficiency

			foreach (var ptx in confirmedTxs.Values)
			{
				spentOutputs.AddRange(ptx.pInputs.Select(t => t.Item1));
			}

			foreach (var item in confirmedTxs)
			{
				// check in ICTxs as well?
				if (memPool.TxPool.Contains(item.Key))
				{
					BlockChainTrace.Information("same tx removed from txpool", item.Value);
					memPool.TxPool.Remove(item.Key);
					memPool.ContractPool.Remove(item.Key);
				}
				else
				{
					new HandleOrphansOfTxAction(item.Key).Publish(); // assume tx is unseen. try to unorphan
				}

				// Make list of **keys** in txpool and ictxpool
				// for each key in list, check if Double Spent. Remove recursively.
				// RemoveIfDoubleSpent is recursive over all pools, then sends a RemoveRef to ContractPool
				memPool.TxPool.RemoveDoubleSpends(spentOutputs);
				memPool.ICTxPool.RemoveDoubleSpends(spentOutputs);

				//new TxMessage(item.Key, item.Value, TxStateEnum.Confirmed).Publish();
			}
		}

		public IsTxOrphanResult IsOrphanTx(TransactionContext dbTx, Types.Transaction tx, out TransactionValidation.PointedTransaction ptx)
		{
			var outputs = new List<Types.Output>();

			ptx = null;

			foreach (Types.Outpoint input in tx.inputs)
			{
				if (UTXOStore.ContainsKey(dbTx, input))
				{
					outputs.Add(UTXOStore.Get(dbTx, input).Value);
				}
				else if (memPool.TxPool.Contains(input.txHash))
				{
					if (input.index < memPool.TxPool[input.txHash].outputs.Length)
					{
						outputs.Add(memPool.TxPool[input.txHash].outputs[(int)input.index]);
					}
					else
					{
						BlockChainTrace.Information("can't construct ptx", tx);
						return IsTxOrphanResult.Invalid;
					}
				}
			}

			if (outputs.Count < tx.inputs.Count())
			{
				return IsTxOrphanResult.Orphan;
			}

			ptx = TransactionValidation.toPointedTransaction(
				tx,
				ListModule.OfSeq<Types.Output>(outputs)
			);

			return IsTxOrphanResult.NotOrphan;
		}

		public bool IsValidTransaction(TransactionContext dbTx, TransactionValidation.PointedTransaction ptx)
		{
			//Verify crypto signatures for each input; reject if any are bad


			//Using the referenced output transactions to get input values, check that each input value, as well as the sum, are in legal money range
			//Reject if the sum of input values < sum of output values


			//for (var i = 0; i < ptx.pInputs.Length; i++)
			//{
			//	if (!TransactionValidation.validateAtIndex(ptx, i))
			//		return false;
			//}

			return true;
		}

		Types.Output GetUTXO(Types.Outpoint outpoint, bool IsInBlock)
		{
			using (TransactionContext dbTx = _DBContext.GetTransactionContext())
			{
				return GetUTXO(outpoint, dbTx, IsInBlock);
			}
		}
					
		Types.Output GetUTXO(Types.Outpoint outpoint, TransactionContext dbTx, bool IsInBlock)
		{
			try
			{
				if (!IsInBlock)
				{
					foreach (var item in memPool.TxPool)
					{
						foreach (var pInput in item.Value.pInputs)
						{
							if (outpoint == pInput.Item1)
								return null;
						}
					}
				}

				var result = UTXOStore.Get(dbTx, outpoint);

				if (result != null)
					return result.Value;

				if (!IsInBlock && memPool.TxPool.Contains(outpoint.txHash))
				{
					var tx = memPool.TxPool[outpoint.txHash];

					if (tx.outputs.Count() > outpoint.index)
					{
						return tx.outputs[(int)outpoint.index];
					}
				}
			}
			catch (Exception e)
			{
				BlockChainTrace.Error("GetUTXO", e);
			}

			BlockChainTrace.Information("Could not find UTXO!");

			return null;
		}

		public bool IsValidTransaction(TransactionContext dbTx, TransactionValidation.PointedTransaction ptx, byte[] contractHash, bool IsInBlock = false)
		{
			var isWitness = false;
			var witnessIdx = -1;
			byte[] message = null;

			for (var i = 0; i < ptx.witnesses.Length; i++)
			{
				if (ptx.witnesses[i].Length > 0)
					witnessIdx = i;
			}

			if (witnessIdx == 0)
			{
				message = ptx.witnesses[0];
			}
			else if (witnessIdx == -1)
			{
				var contractLock = ptx.pInputs[0].Item2.@lock as Types.OutputLock.ContractLock;

				if (contractLock == null)
				{
					BlockChainTrace.Information("expected ContractLock, tx invalid");
					return false;
				}

				message = contractLock.data;
			}

			isWitness = witnessIdx == 0;

			Types.Transaction tx;
			var isExecutionSuccessful = ExecuteContract(dbTx, contractHash, message, out tx, isWitness, IsInBlock);

			return isExecutionSuccessful && tx != null && TransactionValidation.unpoint(ptx).Equals(tx);
		}

		public static IsContractGeneratedTxResult IsContractGeneratedTx(TransactionValidation.PointedTransaction ptx, out byte[] contractHash)
		{
			contractHash = null;

			foreach (var input in ptx.pInputs)
			{
				if (input.Item2.@lock.IsContractLock)
				{
					if (contractHash == null)
						contractHash = ((Types.OutputLock.ContractLock)input.Item2.@lock).contractHash;
					else if (!contractHash.SequenceEqual(((Types.OutputLock.ContractLock)input.Item2.@lock).contractHash))
						return IsContractGeneratedTxResult.Invalid;

					else if (!contractHash.SequenceEqual(((Types.OutputLock.ContractLock)input.Item2.@lock).contractHash))
					{
						BlockChainTrace.Information("Unexpected contactHash", contractHash);
						return IsContractGeneratedTxResult.Invalid;
					}
				}
			}

			return contractHash == null ? IsContractGeneratedTxResult.NotContractGenerated : IsContractGeneratedTxResult.ContractGenerated;
		}


		// TODO replace with two functions:
		// IsContractActive(contractHash), which checks if the contract is in the ACS on disk or in the contractpool;
		// bool IsContractGeneratedTransactionValid(dbtx, contracthash, ptx), which raises an exception if called with a missing contract
		//public static bool IsContractGeneratedTransactionValid(TransactionContext dbTx, TransactionValidation.PointedTransaction ptx, byte[] contractHash)
		//{
		//	var chainTip = new ChainTip().Context(dbTx).Value;
		//	var tipBlockHeader = chainTip == null ? null : new BlockStore().GetBlock(dbTx, chainTip).Value.header;
		//	return xValid(ptx, contractHash, utxos, tipBlockHeader);
		//}

        public void InitBlockTimestamps(TransactionContext dbTx)
		{
			if (Tip != null)
			{
				var timestamps = new List<long>();
				var itr = Tip.Value;

				while (itr != null && timestamps.Count < BlockTimestamps.SIZE)
				{
					timestamps.Add(itr.header.timestamp);

                    if (itr.header.parent.Length == 0)
                    {
                        break;
                    }

                    var bk = BlockStore.GetBlock(dbTx, itr.header.parent);

                    System.Diagnostics.Debug.Assert(bk != null);

                    itr = bk.Value;
				}
				Timestamps.Init(timestamps.ToArray());
			}
		}

		public bool IsTipOld
		{
			get
			{
				return Tip == null ? true : DateTime.Now.ToUniversalTime() - DateTime.FromBinary(Tip.Value.header.timestamp) > OLD_TIP_TIME_SPAN;
			}
		}

		//TODO: refactor
		public Keyed<Types.Block> Tip { get; set; }

		Types.Transaction GetTransaction(byte[] key) //TODO: make concurrent
		{
			if (memPool.TxPool.Contains(key))
			{
				return TransactionValidation.unpoint(memPool.TxPool[key]);
			}
			else
			{
				using (TransactionContext context = _DBContext.GetTransactionContext())
				{
					if (BlockStore.TxStore.ContainsKey(context, key))
					{
						return BlockStore.TxStore.Get(context, key).Value;
					}
				}
			}

			return null;
		}

		//TODO: should asset that the block came from main?
		Types.Block GetBlock(byte[] key)
		{
			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				var location = BlockStore.GetLocation(context, key);

				if (location == LocationEnum.Main || location == LocationEnum.Genesis)
				{
					var bk = BlockStore.GetBlock(context, key);

					return bk == null ? null : bk.Value;
				}

				return null;
			}
		}

		byte[] GetContractCode(byte[] contractHash)
		{
			using (TransactionContext dbTx = _DBContext.GetTransactionContext())
			{
				var result = ContractsTxsStore.Get(dbTx.Transaction, contractHash);

				if (result != null && BlockStore.TxStore.ContainsKey(dbTx, result))
				{
					var transaction = BlockStore.TxStore.Get(dbTx, result).Value;

					if (FSharpOption<Types.ExtendedContract>.get_IsSome(transaction.contract))
					{
						if (transaction.contract.Value.IsContract)
						{
							return (transaction.contract.Value as Types.ExtendedContract.Contract).Item.code;
						}
					}
				}
			}

			return null;
		}

		//public Tuple<ulong, ulong> GetTotalAssets(byte[] contractHash)
		//{
		//	ulong confirmed = 0;
		//	ulong unconfirmed = 0;

		//	using (var dbTx = GetDBTransaction())
		//	{
		//		var x = UTXOStore.All(dbTx, null, false).ToList();

		//		foreach (var item in UTXOStore.All(dbTx, null, false).Where(t =>
		//		{
		//			var contractLock = t.Item2.@lock as Types.OutputLock.ContractLock;
		//			return contractLock != null; // && contractLock.contractHash.SequenceEqual(contractHash);
		//		}))
		//		{
		//			confirmed += item.Item2.spend.amount;
		//		}
		//	}

		//	foreach (var item in memPool.TxPool)
		//	{
		//		foreach (var output in item.Value.outputs)
		//		{
		//			var contractLock = output.@lock as Types.OutputLock.ContractLock;
		//			if (contractLock != null)
		//			{
		//				confirmed += output.spend.amount;
		//			}
		//		}
		//	}

		//	return new Tuple<ulong, ulong>(confirmed, unconfirmed);
		//}

		// TODO: use linq, return enumerator, remove predicate
		void GetUTXOSet(Func<Types.Output, bool> predicate, out HashDictionary<List<Types.Output>> txOutputs, out HashDictionary<Types.Transaction> txs)
		{
			txOutputs = new HashDictionary<List<Types.Output>>();
			txs = new HashDictionary<Types.Transaction>();

			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				foreach (var item in UTXOStore.All(context, predicate, true))
				{
					if (!txOutputs.ContainsKey(item.Item1.txHash))
					{
						txOutputs[item.Item1.txHash] = new List<Types.Output>();
					}

					txOutputs[item.Item1.txHash].Add(item.Item2);
					txs[item.Item1.txHash] = BlockStore.TxStore.Get(context, item.Item1.txHash).Value;
				}
			}
		}

		bool ExecuteContract(byte[] contractHash, byte[] message, out Types.Transaction transaction, bool isWitness, bool isInBlock)
		{
			using (TransactionContext dbTx = _DBContext.GetTransactionContext())
			{
				return ExecuteContract(dbTx, contractHash, message, out transaction, isWitness, isInBlock);
			}
		}

		bool ExecuteContract(TransactionContext dbTx, byte[] contractHash, byte[] message, out Types.Transaction transaction, bool isWitness, bool isInBlock)
		{
			try
			{
				if (!new ActiveContractSet().IsActive(dbTx, contractHash))
				{
					transaction = null;
					return false;
				}

				Func<Types.Outpoint, FSharpOption<Types.Output>> getUTXO = t =>
				{
                    return new FSharpOption<Types.Output>(GetUTXO(t, dbTx, isInBlock));
				};

				var contractArgs = new ContractArgs()
				{
					ContractHash = contractHash,
					Message = message,
					tryFindUTXOFunc = getUTXO
				};

				var acsItem = new ActiveContractSet().Get(dbTx, contractHash);

				if (acsItem.Value != null)
				{
					var func = ContractExamples.Execution.deserialize(acsItem.Value.CompiledContract);

					var result = func.Invoke(new Tuple<byte[], byte[], FSharpFunc<Types.Outpoint, FSharpOption<Types.Output>>>(
						contractArgs.Message,
						contractArgs.ContractHash,
						FSharpFunc<Types.Outpoint, FSharpOption<Types.Output>>.FromConverter(t => contractArgs.tryFindUTXOFunc(t))));

					var txSkeleton = result as Tuple<FSharpList<Types.Outpoint>, FSharpList<Types.Output>, byte[]>;

					transaction = txSkeleton == null || txSkeleton.Item2.Count() == 0 ? null :
						new Types.Transaction(
							Tests.tx.version,
							txSkeleton.Item1,
							ListModule.OfSeq<byte[]>(isWitness ? new byte[][] { contractArgs.Message } : new byte[][] { }),
							txSkeleton.Item2,
							FSharpOption<Types.ExtendedContract>.None //TODO: get from txSkeleton.Item3
						);

					return true;
				}
			}
			catch (Exception e)
			{
				BlockChainTrace.Error("Error executing contract", e);
			}

			transaction = null;
			return false;
		}

		List<ACSItem> GetActiveContacts()
		{
			using (var dbTx = _DBContext.GetTransactionContext())
			{
				return new ActiveContractSet().All(dbTx).Select(t => t.Item2).ToList();
			}
		}

		List<Tuple<Types.Outpoint, Types.Output>> GetContractPointedOutputs(byte[] contractHash)
		{
			var result = new List<Tuple<Types.Outpoint, Types.Output>>();

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				foreach (var item in UTXOStore.All(dbTx, t => t.@lock is Types.OutputLock.ContractLock, false))
				{
					var lockContractHash = ((Types.OutputLock.ContractLock)item.Item2.@lock).contractHash;

					if (contractHash.SequenceEqual(lockContractHash))
						result.Add(item);
				}
			}

			foreach (var item in memPool.TxPool)
			{
				uint i = 0;
				foreach (var output in item.Value.outputs)
				{
					if (output.@lock is Types.OutputLock.ContractLock)
					{
						var lockContractHash = ((Types.OutputLock.ContractLock)output.@lock).contractHash;

						if (contractHash.SequenceEqual(lockContractHash))
						{
							result.Add(new Tuple<Types.Outpoint, Types.Output>(new Types.Outpoint(item.Key, i), output));
						}
					}

					i++;
				}
			}

			foreach (var item in memPool.TxPool)
			{
				foreach (var input in item.Value.pInputs)
				{
					result.RemoveAll(t => t.Item1.Equals(input.Item1));
				}
			}

			return result;
		}

		bool IsContractActive(byte[] contractHash)
		{
			using (var dbTx = _DBContext.GetTransactionContext())
			{
				return new ActiveContractSet().IsActive(dbTx, contractHash);
			}

			//TODO: get number of blocks
			//var lastBlock = new ActiveContractSet().LastBlock(dbTx, contractHash);
			//var currentHeight = Tip == null ? 0 : Tip.Value.header.blockNumber;
			//return nextBlocks > currentHeight - lastBlock;
		}

		bool IsConfirmedUTXOExist(Types.Outpoint outpoint)
		{
			using (var dbTx = _DBContext.GetTransactionContext())
			{
				return UTXOStore.ContainsKey(dbTx, outpoint);
			}
		}
	}
}
