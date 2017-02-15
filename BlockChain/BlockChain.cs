using System;
using Consensus;
using BlockChain.Store;
using Store;
using Infrastructure;
using System.Collections.Generic;
using Microsoft.FSharp.Collections;
using System.Linq;
using BlockChain.Data;

namespace BlockChain
{
	public class BlockChain : ResourceOwner
	{
		readonly TimeSpan OLD_TIP_TIME_SPAN = TimeSpan.FromMinutes(5);
		readonly DBContext _DBContext;

		public TxPool pool { get; set; }
	//	public ContractPool ContractsMempool { get; set; }
		public UTXOStore UTXOStore { get; set; }
		public BlockStore BlockStore { get; set; }
	//	public ContractStore ContractStore { get; set; }
	//	public ActiveContractSet ACS { get; set; }
		public BlockNumberDifficulties BlockNumberDifficulties { get; set; }
		public ChainTip ChainTip { get; set; }
		public BlockTimestamps Timestamps { get; set; }
		public byte[] GenesisBlockHash { get; set; }

		public BlockChain(string dbName, byte[] genesisBlockHash)
		{
			_DBContext = new DBContext(dbName);
			pool = new TxPool();
			UTXOStore = new UTXOStore();
			BlockStore = new BlockStore();
		//	ContractStore = new ContractStore();
		//	ACS = new ActiveContractSet();
		//	ContractsMempool = new ContractPool();
			BlockNumberDifficulties = new BlockNumberDifficulties();
			ChainTip = new ChainTip();
			Timestamps = new BlockTimestamps();
			GenesisBlockHash = genesisBlockHash;

			using (var context = _DBContext.GetTransactionContext())
			{
				var chainTip = ChainTip.Context(context).Value;

				//TODO: check if makred as main?
				Tip = chainTip == null ? null : BlockStore.GetBlock(context, chainTip);
			}

			InitBlockTimestamps();

			OwnResource(MessageProducer<QueueAction>.Instance.AddMessageListener(new EventLoopMessageListener<QueueAction>(ProcessAction)));
			OwnResource(_DBContext);
		}

		void ProcessAction(QueueAction action)
		{
			if (action is HandleBlockAction) 
				HandleBlock(action as HandleBlockAction);
			else if (action is HandleOrphansOfTxAction) 
				HandleOrphansOfTransaction(action as HandleOrphansOfTxAction);
		}

		void HandleOrphansOfTransaction(HandleOrphansOfTxAction a)
		{
			BlockChainTrace.Information("Tx orphans check ");

			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				foreach (var tx in pool.GetOrphansOf(a.TxHash))
				{
					TransactionValidation.PointedTransaction ptx;
					if (!IsOrphanTx(context, tx.Item2, out ptx) && IsValidTransaction(context, ptx))
					{
						AddToMempool(tx.Item1, ptx);
					}
				}
			}
		}

		/// <summary>
		/// Handles a new transaction from network or wallet. 
		/// </summary>
		/// <returns><c>true</c>, if new transaction was acceped, <c>false</c> rejected.</returns>
		public bool HandleTransaction(Types.Transaction tx)
		{
			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				TransactionValidation.PointedTransaction ptx;
				var txHash = Merkle.transactionHasher.Invoke(tx);

				if (pool.ContainsKey(txHash))
				{
					BlockChainTrace.Information("Tx already in mempool");
					return false;
				}

				if (BlockStore.TxStore.ContainsKey(context, txHash))
				{
					BlockChainTrace.Information("Tx already in store");
					return false;
				}

				if (pool.ContainsInputs(tx))
				{
					BlockChainTrace.Information("Mempool contains spending input");
					return false;
				}

				if (IsOrphanTx(context, tx, out ptx))
				{
					BlockChainTrace.Information("Tx added as orphan");
					pool.AddOrphan(txHash, tx);
					return true;
				}

				//TODO: 5. For each input, if the referenced transaction is coinbase, reject if it has fewer than COINBASE_MATURITY confirmations.
				//TODO: 7. Apply fee rules. If fails, reject
				//TODO: 8. Validate each input. If fails, reject

				if (!IsContractGeneratedTransactionValid(context, ptx))
					return false;

				if (!IsValidTransaction(context, ptx))
				{
					BlockChainTrace.Information("invalid inputs");
					return false;
				}

				AddToMempool(txHash, ptx);
				return true;
			}
		}

		void AddToMempool(byte[] txHash, TransactionValidation.PointedTransaction ptx)
		{
			BlockChainTrace.Information("Transaction added to mempool");
			pool.Lock(delegate {
				pool.Add(txHash, ptx);
			});
			new NewTxMessage(txHash, ptx, TxStateEnum.Unconfirmed).Publish();
			new HandleOrphansOfTxAction(txHash).Publish();
		}

		/// <summary>
		/// Handles a block from network.
		/// </summary>
		/// <returns><c>true</c>, if new block was acceped, <c>false</c> rejected.</returns>
		public bool HandleBlock(Types.Block bk)
		{
			return HandleBlock(new HandleBlockAction(bk));
		}

		bool HandleBlock(HandleBlockAction a)
		{
			var queuedActions = new List<QueueAction>();
			BlockVerificationHelper.ResultEnum result;

			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				var txs = new HashDictionary<Tuple<Types.Transaction, bool>>();

				result = new BlockVerificationHelper(
					this,
					context,
					a.BkHash,
					a.Bk,
					txs,
					queuedActions,
					a.IsOrphan
				).Result;
				
				switch (result)
				{
					case BlockVerificationHelper.ResultEnum.AddedOrphan:
						context.Commit();
						break;
					case BlockVerificationHelper.ResultEnum.Added:
					case BlockVerificationHelper.ResultEnum.Reorganization:
						if (txs.Count > 0)
						{
							pool.Lock(() =>
							{
								if (result != BlockVerificationHelper.ResultEnum.Reorganization)
								{
									context.Commit();
								}
								ApplyToMempool(context, txs);
							});
						}
						else
						{
							context.Commit();
						}
						break;
					case BlockVerificationHelper.ResultEnum.Rejected:
						return false;
				}

				foreach (var _bk in BlockStore.Orphans(context, a.BkHash))
				{
					new HandleBlockAction(_bk.Key, _bk.Value, true).Publish();
				}
			}

			foreach (var action in queuedActions)
			{
				if (action is MessageAction)
					(action as MessageAction).Message.Publish();
				else
					action.Publish();
			}

			return true;
		}

		void EvictToMempool(TransactionContext dbTx, byte[] txHash, Types.Transaction tx)
		{
			TransactionValidation.PointedTransaction ptx;

			if (!IsOrphanTx(dbTx, tx, out ptx) && IsValidTransaction(dbTx, ptx))
			{
				AddToMempool(txHash, ptx);

				byte[] contractHash;
				if (IsContractActivatingTx(ptx, out contractHash))
				{
					//ContractsMempool.Add(contractHash);
				}
				return;
			}
			else
			{
				ClearMempoolRecursive(txHash);
				ClearContractsRecursive(txHash);
				new NewTxMessage(txHash, ptx, TxStateEnum.Invalid).Publish();
			}

			new NewTxMessage(txHash, ptx, TxStateEnum.Unconfirmed).Publish();
		}

		void ClearMempoolRecursive(byte[] txHash)
		{
			foreach (var item in pool.GetDependencies(txHash))
			{
				new NewTxMessage(item.Item1, item.Item2, TxStateEnum.Invalid).Publish();
				ClearMempoolRecursive(item.Item1);
			}
		}

		void ClearContractsRecursive(byte[] txHash)
		{
			foreach (var item in pool.GetDependencies(txHash, pool.ICTxs))
			{
				//ContractsMempool.RemoveTx(txHash);

				//new NewTxMessage(item.Item1, item.Item2, TxStateEnum.Invalid).Publish();
				//ClearMempoolRecursive(item.Item1);
			}
		}

		void ApplyToMempool(TransactionContext dbTx, HashDictionary<Tuple<Types.Transaction, bool>> txs)
		{
			// unconfirmed txs - into mempool
			foreach (var item in txs.Where(t => !t.Value.Item2))
			{
				EvictToMempool(dbTx, item.Key, item.Value.Item1);
			}

			// confirmed txs
			foreach (var item in txs.Where(t => t.Value.Item2))
			{
				if (pool.ContainsKey(item.Key))
				{
					BlockChainTrace.Information("same tx removed from txpool");
					pool.Remove(item.Key);
				}
				else
				{
					// if can't remove - assume tx is unseen. try to unorphan
					new HandleOrphansOfTxAction(item.Key).Publish();
				}

				pool.GetTransactionsInConflict(item.Value.Item1).ToList().ForEach(t =>
				{
					BlockChainTrace.Information("invalidated tx removed from txpool");
					var removed = new List<byte[]>();
					pool.Remove(t.Item1, removed);
					removed.ForEach(txHash => new MessageAction(new NewTxMessage(txHash, TxStateEnum.Invalid)).Publish());
				});

				pool.ContractPool.RemoveRef(item.Key, dbTx, pool);

				new MessageAction(new NewTxMessage(item.Key, TxStateEnum.Confirmed)).Publish();
			}
		}

		public bool IsContractActivatingTx(TransactionValidation.PointedTransaction ptx, out byte[] contractHash)
		{
			contractHash = null;
			//foreach (var output in ptx.outputs)
			//{
			//	if (output.@lock.IsContractSacrificeLock)
			//	{
			//		return true;
			//	}
			//}
			return false;
		}

		public bool IsOrphanTx(TransactionContext dbTx, Types.Transaction tx, out TransactionValidation.PointedTransaction ptx)
		{
			ptx = null;
			var outputs = new List<Types.Output>();

			foreach (Types.Outpoint input in tx.inputs)
			{
				byte[] newArray = new byte[input.txHash.Length + 1];
				input.txHash.CopyTo(newArray, 0);
				newArray[input.txHash.Length] = (byte)input.index;

				if (!UTXOStore.ContainsKey(dbTx, newArray)) //TODO: refactor ContainsKey
				{
					if (!pool.ContainsKey(input.txHash))
					{
						return true;
					}
					else
					{
						outputs.Add(pool.Get(input.txHash).pInputs[(int)input.index].Item2);
					}
				}
				else
				{
					outputs.Add(UTXOStore.Get(dbTx, newArray).Value);
				}
			}

			ptx = TransactionValidation.toPointedTransaction(
				tx,
				ListModule.OfSeq<Types.Output>(outputs)
			);

			return false;
		}

		public bool IsValidTransaction(TransactionContext dbTx, TransactionValidation.PointedTransaction ptx)
		{
			//For each input, if the referenced output transaction is coinbase (i.e.only 1 input, with hash = 0, n = -1), it must have at least COINBASE_MATURITY (100) confirmations; else reject.
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

		public static bool IsContractGeneratedTx(TransactionValidation.PointedTransaction ptx, out byte[] contractHash)
		{
			contractHash = null;

			foreach (var input in ptx.pInputs)
			{
				if (input.Item2.@lock.IsContractLock)
				{
					if (contractHash == null)
						contractHash = ((Types.OutputLock.ContractLock)input.Item2.@lock).contractHash;

					else if (!contractHash.SequenceEqual(((Types.OutputLock.ContractLock)input.Item2.@lock).contractHash))
					{
						BlockChainTrace.Information("Unexpected contactHash");
						return false;
					}
				}
				else return false;
			}

			return contractHash != null;
		}

		public bool IsContractGeneratedTransactionValid(TransactionContext dbTx, TransactionValidation.PointedTransaction ptx)
		{
			byte[] contractHash = null;

			if (IsContractGeneratedTx(ptx, out contractHash))
			{
				if (new ActiveContractSet().IsActive(dbTx, contractHash))
				{

					/// 
					/// 
					/// 
					/// 
					/// 
					/// TODO: cache

					var utxos = new List<Tuple<Types.Outpoint, Types.Output>>();

					using (var context = _DBContext.GetTransactionContext())
					{
						foreach (var item in UTXOStore.All(context, null, false))
						{
							byte[] txHash = new byte[item.Key.Length - 1];
							Array.Copy(item.Key, txHash, txHash.Length);
							var index = Convert.ToUInt32(item.Key[item.Key.Length - 1]);

							utxos.Add(new Tuple<Types.Outpoint, Types.Output>(new Types.Outpoint(txHash, index), item.Value));
						}
					}


					var args = new ContractArgs()
					{
						context = new Types.ContractContext(contractHash, new FSharpMap<Types.Outpoint, Types.Output>(utxos)),
						//		inputs = inputs,
						witnesses = new List<byte[]>(),
						outputs = ptx.outputs.ToList(),
						option = Types.ExtendedContract.NewContract(null)
					};

					/// 
					/// 
					/// 
					/// 
					/// 


					Types.Transaction tx;
					if (!ContractHelper.Execute(contractHash, out tx, args))
					{
						BlockChainTrace.Information("Contract execution failed");
						return false;
					}

					return TransactionValidation.unpoint(ptx).Equals(tx);
				}
				else
				{
					BlockChainTrace.Information("Contract not active");
					return false;
				}
			}

			return true;
		}

		public TransactionContext GetDBTransaction()
		{
			return _DBContext.GetTransactionContext();
		}

		public void InitBlockTimestamps()
		{
			if (Tip != null)
			{
				var timestamps = new List<long>();
				var itr = Tip == null ? null : Tip.Value;

				while (itr != null && timestamps.Count < BlockTimestamps.SIZE)
				{
					timestamps.Add(itr.header.timestamp);
					itr = itr.header.parent.Length == 0 ? null : GetBlock(itr.header.parent);
				}
				Timestamps.Init(timestamps.ToArray());
			}
		}

		public bool IsTipOld
		{
			get
			{
				return Tip == null ? true : DateTime.Now - DateTime.FromBinary(Tip.Value.header.timestamp) > OLD_TIP_TIME_SPAN;
			}
		}

		//TODO: refactor
		public Keyed<Types.Block> Tip { get; set; }

		public Types.Transaction GetTransaction(byte[] key) //TODO: make concurrent
		{
			if (pool.ContainsKey(key))
			{
				return TransactionValidation.unpoint(pool.Get(key));
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
		public Types.Block GetBlock(byte[] key)
		{
			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				var bk = BlockStore.GetBlock(context, key);

				return bk == null ? null : bk.Value;
			}
		}

		// TODO: use linq, return enumerator, remove predicate
		public Dictionary<Keyed<Types.Transaction>, Types.Output> GetUTXOSet(Func<Types.Output, bool> predicate)
		{
			var outputs = new HashDictionary<Types.Output>();
			var values = new Dictionary<Keyed<Types.Transaction>, Types.Output>();

			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				foreach (var output in UTXOStore.All(context, predicate, true))
				{
					byte[] txHash = new byte[output.Key.Length - 1];
					Array.Copy(output.Key, txHash, txHash.Length);
					outputs[txHash] = output.Value;
				}

				foreach (var output in outputs)
				{
					var tx = BlockStore.TxStore.Get(context, output.Key);
					values[tx] = output.Value;
				}
			}

			return values;
		}

		////demo
		public Types.Block MineAllInMempool()
		{
			if (pool.Transactions.Count == 0)
			{
				return null;
			}

			uint version = 1;
			string date = "2000-02-02";

		//	Merkle.Hashable x = new Merkle.Hashable ();
		//	x.
		//	var merkleRoot = Merkle.merkleRoot(Tip.Key,

			var nonce = new byte[10];

			new Random().NextBytes (nonce);

			var blockHeader = new Types.BlockHeader(
				version,
				Tip.Key,
				0,
				new byte[] { },
				new byte[] { },
				new byte[] { },
				ListModule.OfSeq<byte[]>(new List<byte[]>()),
				DateTime.Parse(date).ToBinary(),
				1,
				nonce
			);

			var newBlock = new Types.Block(blockHeader, ListModule.OfSeq<Types.Transaction>(pool.Transactions.Select(
				t => TransactionValidation.unpoint(t.Value)))
          	);

			if (HandleBlock(newBlock))
			{
				return newBlock;
			}
			else 
			{
				BlockChainTrace.Information("*** error mining block ***");
				//	throw new Exception();
				return null;
			}
		}
	}
}
