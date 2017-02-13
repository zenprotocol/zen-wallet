using Store;
using BlockChain.Store;
using Consensus;
using System;
using System.Linq;
using System.Collections.Generic;
using BlockChain.Data;

namespace BlockChain
{
	public class BlockVerificationHelper
	{
		public ResultEnum Result { get; set; }

		BlockChain _BlockChain;
		TransactionContext _DbTx;
		byte[] _BkHash;
		Types.Block _Bk;
		HashDictionary<Tuple<Types.Transaction, bool>> _MempoolTxs;

		public enum ResultEnum
		{
			Added,
			Reorganization,
			AddedOrphan,
			Rejected
		}

		public BlockVerificationHelper(
			BlockChain blockChain,
			TransactionContext dbTx,
			byte[] bkHash,
			Types.Block bk,
			HashDictionary<Tuple<Types.Transaction, bool>> txs,
			List<QueueAction> queuedActions, 
			bool handleOrphan = false, 
			bool handleBranch = false
		)
		{
			_BlockChain = blockChain;
			_DbTx = dbTx;
			_BkHash = bkHash;
			_Bk = bk;
			_MempoolTxs = txs;
			
			if (!IsValid())
			{
				BlockChainTrace.Information("not valid");
				Result = ResultEnum.Rejected;
				return;
			}

			if (IsInStore())
			{
				var reject = false;

				switch (blockChain.BlockStore.GetLocation(dbTx, bkHash))
				{
					case LocationEnum.Branch:
						reject = !handleBranch;
						break;
					case LocationEnum.Orphans:
						reject = !handleOrphan && !handleBranch;
						break;
					default:
						reject = true;
						break;
				}

				if (reject)
				{
					BlockChainTrace.Information("block already in store: " + blockChain.BlockStore.GetLocation(dbTx, bkHash));
					Result = ResultEnum.Rejected;
					return;
				}
			}

			//TODO:
			/*
			3. Transaction list must be non - empty
			4. Block hash must satisfy claimed nBits proof of work
			5. Block timestamp must not be more than two hours in the future
			6. First transaction must be coinbase, the rest must not be
			7. For each transaction, apply "tx" checks 2 - 4
			8. (omitted)
			9. (omitted)
			10. Verify Merkle hash
			*/

			if (IsGenesis())
			{
				if (!IsGenesisValid())
				{
					BlockChainTrace.Information("invalid genesis block");
					Result = ResultEnum.Rejected;
					return;
				}
				else
				{
					blockChain.Timestamps.Init(bk.header.timestamp);
					ExtendMain(queuedActions, 0);
					Result = ResultEnum.Added;
					return;
				}
			}

			if (IsOrphan())
			{
				blockChain.BlockStore.Put(dbTx, new Keyed<Types.Block>(bkHash, bk), LocationEnum.Orphans, 0);
				BlockChainTrace.Information("Bk added as orphan");
				Result = ResultEnum.AddedOrphan;
				return;
			}

			//12. Check that nBits value matches the difficulty rules

			if (!IsValidDifficulty() || !IsValidBlockNumber() || !IsValidTimeStamp())
			{
				Result = ResultEnum.Rejected;
				return;
			}

			//14. For certain old blocks(i.e.on initial block download) check that hash matches known values

			var totalWork = TotalWork();

			if (handleBranch) // make a branch block main
			{
				if (!ExtendMain(queuedActions, totalWork))
				{
					Result = ResultEnum.Rejected;
					return;
				}
			}
			else if (IsBranch())
			{
				if (IsNewGreatestWork(totalWork))
				{
					//if (!IsTransactionsValid(pointedTransactions))
					//{
					//	Result = ResultEnum.Rejected;
					//	return;
					//}

					BlockChainTrace.Information($"block {bk.header.blockNumber} extends a branch with new difficulty.");

					var originalTip = blockChain.Tip;

					Keyed<Types.Block> fork = null;
					var newMainChain = GetNewMainChainStartFromForkToLeaf(new Keyed<Types.Block>(bkHash, bk), out fork);

					blockChain.ChainTip.Context(dbTx).Value = fork.Key;
					blockChain.Tip = fork;

					var oldMainChain = GetOldMainChainStartFromLeafToFork(fork, originalTip.Key);

					foreach (var block in oldMainChain)
					{
						BlockChainTrace.Information("undo block in old main chain: " + block.Value.header.blockNumber);
						UndoBlock(block.Value, block.Key);
					}

					//append new chain
					foreach (var _bk in newMainChain)
					{
						BlockChainTrace.Information("try add block to new main chain: " + _bk.Value.header.blockNumber);

						if (new BlockVerificationHelper(
							blockChain,
							dbTx,
							_bk.Key,
							_bk.Value,
							txs,
							queuedActions, 
							false, 
							true).Result == ResultEnum.Rejected)
						{
							blockChain.ChainTip.Context(dbTx).Value = originalTip.Key;
							blockChain.Tip = originalTip;
							blockChain.InitBlockTimestamps();

							Result = ResultEnum.Rejected;
							return;
						}
					}

					foreach (var block in newMainChain)
					{
						MarkMempoolTxs(block.Value, true);
					}
				}
				else
				{
					blockChain.BlockStore.Put(dbTx, new Keyed<Types.Block>(bkHash, bk), LocationEnum.Branch, totalWork);
				}
			}
			else
			{
				if (!ExtendMain(queuedActions, totalWork))
				{
					Result = ResultEnum.Rejected;
					return;
				}
			}

			Result = ResultEnum.Added;
		}

		bool ExtendMain(List<QueueAction> queuedActions, double totalWork)
		{
			if (_BlockChain.BlockStore.ContainsKey(_DbTx, _BkHash))
			{
				_BlockChain.BlockStore.SetLocation(_DbTx, _BkHash, LocationEnum.Main);
			}
			else
			{
				_BlockChain.BlockStore.Put(_DbTx, new Keyed<Types.Block>(_BkHash, _Bk), LocationEnum.Main, totalWork);
			}

			_BlockChain.Timestamps.Push(_Bk.header.timestamp);

			if (_Bk.header.blockNumber % 2000 == 0)
			{
				_BlockChain.BlockNumberDifficulties.Add(_DbTx.Transaction, _Bk.header.blockNumber, _BkHash);
			}

			var blockUndoData = new BlockUndoData();

			var pointedTransactions = new List<TransactionValidation.PointedTransaction>();

			foreach (var transaction in _Bk.transactions)
			{
				TransactionValidation.PointedTransaction ptx;

				if (!IsTransactionValid(transaction, out ptx))
				{
					return false;
				}

				pointedTransactions.Add(ptx);

				var txHash = Merkle.transactionHasher.Invoke(transaction);

				BlockChainTrace.Information("new tx");

				_BlockChain.BlockStore.TxStore.Put(_DbTx, new Keyed<Types.Transaction>(txHash, transaction));

				var i = 0;
				foreach (var input in ptx.pInputs)
				{
					//TODO: refactoring is needed.
					_BlockChain.UTXOStore.Remove(_DbTx, GetOutputKey(input.Item1.txHash, (int)input.Item1.index));

					//TODO: if added, and now removed - just remove from 'added' //if (blockUndoData.AddedUTXO.Contains
					blockUndoData.RemovedUTXO.Add(new Tuple<Types.Outpoint, Types.Output>(input.Item1, input.Item2));
					i++;
				}

				i = 0;
				foreach(var output in ptx.outputs)
				{
					BlockChainTrace.Information($"new utxo, amount {output.spend.amount}");

					// extending a contract?
					if (output.@lock.IsContractSacrificeLock)
					{
						if (!output.spend.asset.SequenceEqual(Tests.zhash))
							continue; // not Zen

						var contractLock = (Types.OutputLock.ContractSacrificeLock)output.@lock;

						if (contractLock.IsHighVLock)
							continue; // not current version

						byte[] contractHash = null;

						if (contractLock.Item.lockData.Length > 0)
						{
							contractHash = contractLock.Item.lockData[0]; // lock-level indicated contract
						}
						else if (transaction.contract.Value != null)
						{
							if (transaction.contract.Value.IsHighVContract)
								continue; // not current version

							contractHash = Merkle.hashHasher.Invoke(((Types.ExtendedContract.Contract)transaction.contract.Value).Item.code); // tx-level indicated contract
						}

						// is contract active (with current block as tip)
						if (_BlockChain.ACS.IsActive(_DbTx, contractHash, _Bk.header.blockNumber))
						{
							// snapshot only if first time (not snapshoted before)
							if (!blockUndoData.ACSDeltas.ContainsKey(contractHash))
							{
								blockUndoData.ACSDeltas.Add(contractHash,
									  _BlockChain.ACS.Get(_DbTx, contractHash).Value);
							}

							// extend
							_BlockChain.ACS.Extend(_DbTx, contractHash, output.spend.amount);
						}
					}

					if (output.@lock.IsPKLock || output.@lock.IsContractLock)
					{
						_BlockChain.UTXOStore.Put(_DbTx, new Keyed<Types.Output>(GetOutputKey(txHash, i), output));
						blockUndoData.AddedUTXO.Add(new Tuple<Types.Outpoint, Types.Output>(new Types.Outpoint(txHash, (uint)i), output));
					}
					i++;
				}
			}

			var purgedList = _BlockChain.ACS.GetExpiringList(_DbTx, _Bk.header.blockNumber);

			foreach (var purged in purgedList)
			{
				blockUndoData.ACSDeltas.Add(purged,
              		_BlockChain.ACS.Get(_DbTx, purged).Value);
			}

			_BlockChain.ACS.DeactivateContracts(_DbTx, _Bk.header.blockNumber, out purgedList);

			ValidateACS();

			_BlockChain.ChainTip.Context(_DbTx).Value = _BkHash;
			_BlockChain.Tip = new Keyed<Types.Block>(_BkHash, _Bk);

			MarkMempoolTxs(_Bk, true);

			queuedActions.Add(new MessageAction(new NewBlockMessage(pointedTransactions, true)));

			return true;
		}

		bool IsValid()
		{
			return true;
		}

		bool IsCoinbase(Types.Transaction tx)
		{
			return false;
		}

		bool IsGenesis()
		{
			return _BkHash.SequenceEqual(_BlockChain.GenesisBlockHash);
		}

		bool IsOrphan()
		{
			return !_BlockChain.BlockStore.ContainsKey(_DbTx, _Bk.header.parent);
		}

		bool IsInStore()
		{
			return _BlockChain.BlockStore.ContainsKey(_DbTx, _BkHash);
		}

		bool IsBranch()
		{
			return 
				_BlockChain.BlockStore.IsLocation(_DbTx, _Bk.header.parent, LocationEnum.Branch) ||
				_BlockChain.BlockStore.HasChildren(_DbTx, _Bk.header.parent);
		}

		bool IsTransactionValid(Types.Transaction tx, out TransactionValidation.PointedTransaction ptx)
		{
			if (_BlockChain.IsOrphanTx(_DbTx, tx, out ptx))
				return false;

			if (!_BlockChain.IsContractGeneratedTransactionValid(_DbTx, ptx))
				return false;

			if (!_BlockChain.IsValidTransaction(_DbTx, ptx))
				return false;

			return true;
		}

		bool IsValidTimeStamp()
		{
			var result = _Bk.header.timestamp > _BlockChain.Timestamps.Median();

			if (!result)
				BlockChainTrace.Information("invalid timestamp");
			
			return result;
		}

		bool IsValidDifficulty()
		{
			UInt32 expectedDifficulty;

			if (_Bk.header.blockNumber % 2000 == 1)
			{
				var lastBlockHash = _BlockChain.BlockNumberDifficulties.GetLast(_DbTx.Transaction);
				var lastBlock = GetBlock(lastBlockHash);
				expectedDifficulty = NewDifficulty(lastBlock.Value.header.timestamp, _Bk.header.timestamp);
			}
			else
			{
				var tip = _BlockChain.ChainTip.Context(_DbTx).Value;
				var tipBlock = GetBlock(tip).Value;
				expectedDifficulty = tipBlock.header.pdiff;
			}

			var blockDifficulty = _Bk.header.pdiff;

#if TRACE
			if (blockDifficulty != expectedDifficulty)
			{
				BlockChainTrace.Information($"expecting difficulty {expectedDifficulty}, found {blockDifficulty}");
			}
#endif

			return blockDifficulty == expectedDifficulty;
		}

		bool IsValidBlockNumber()
		{
			var parentBlock = GetBlock(_Bk.header.parent).Value;

#if TRACE
			if (parentBlock.header.blockNumber >= _Bk.header.blockNumber)
			{
				BlockChainTrace.Information($"expecting block-number greater than {parentBlock.header.blockNumber}, found {_Bk.header.blockNumber}");
			}
#endif
			return parentBlock.header.blockNumber < _Bk.header.blockNumber;
		}

		UInt32 NewDifficulty(long startTime, long endTime)
		{
			return 0;
		}

		//TODO: use merkle-root validation 
		internal bool ValidateACS()
		{
			return true;
		}

		byte[] GetOutputKey(Types.Transaction transaction, int index) //TODO: convert to outpoint
		{
			return GetOutputKey(Merkle.transactionHasher.Invoke(transaction), index);
		}

		byte[] GetOutputKey(byte[] txHash, int index) //TODO: convert to outpoint
		{
			byte[] outputKey = new byte[txHash.Length + 1];
			txHash.CopyTo(outputKey, 0);
			outputKey[txHash.Length] = (byte)index;

			return outputKey;
		}

		double TotalWork()
		{
			return 1000 * _Bk.header.blockNumber;
			//var parentTotalWork = _BlockChain.BlockStore.TotalWork(_DbTx, _Bk.header.parent);
			
			//return TransactionValidation.totalWork(
			//	parentTotalWork,
			//	_Bk.header.pdiff
			//);
		}

		bool IsNewGreatestWork(double totalWork)
		{
			var tip = _BlockChain.ChainTip.Context(_DbTx).Value;
			var tipBlock = GetBlock(tip);
			var tipTotalWork = 1000 * tipBlock.Value.header.blockNumber; // _BlockChain.BlockStore.TotalWork(_DbTx, tipBlock.Key);

			return totalWork > tipTotalWork;
		}

		bool IsGenesisValid()
		{
			return true;
		}

		Keyed<Types.Block> GetBlock(byte[] _BkHash)
		{
			return _BlockChain.BlockStore.GetBlock(_DbTx, _BkHash);
		}

		List<Keyed<Types.Block>> GetNewMainChainStartFromForkToLeaf(Keyed<Types.Block> leaf, out Keyed<Types.Block> fork)
		{
			var list = new List<Keyed<Types.Block>>();

			do
			{
				list.Insert(0, leaf);
				leaf = GetBlock(leaf.Value.header.parent);
			} while (_BlockChain.BlockStore.IsLocation(_DbTx, leaf.Key, LocationEnum.Branch));

			fork = leaf;

			return list;
		}

		List<Keyed<Types.Block>> GetOldMainChainStartFromLeafToFork(Keyed<Types.Block> fork, byte[] tip)
		{
			var itr = GetBlock(tip);
			var list = new List<Keyed<Types.Block>>();

			while (itr.Value.header.parent.Length > 0 && !itr.Key.SequenceEqual(fork.Key))
			{
				list.Add(itr);
				itr = GetBlock(itr.Value.header.parent);
			}

			return list;
		}

		void UndoBlock(Types.Block block, byte[] key)
		{
			_BlockChain.BlockStore.SetLocation(_DbTx, key, LocationEnum.Branch);

			//undo utxos
			foreach (var tx in _BlockChain.BlockStore.Transactions(_DbTx, key))
			{
				for (var i = 0; i < tx.Value.outputs.Length; i++)
				{
					var blockUndoData = _BlockChain.BlockStore.GetUndoData(_DbTx, key);

					blockUndoData.AddedUTXO.ForEach(u =>
					{
						_BlockChain.UTXOStore.Remove(_DbTx, GetOutputKey(u.Item1.txHash, (int)u.Item1.index));
					});

					blockUndoData.RemovedUTXO.ForEach(u =>
					{
						_BlockChain.UTXOStore.Put(_DbTx, new Keyed<Types.Output>(GetOutputKey(u.Item1.txHash, (int)u.Item1.index), u.Item2));
					});

					//TODO: undo ASC
				}
			}

			MarkMempoolTxs(block, false);
		}

		void MarkMempoolTxs(Types.Block block, bool confirmed)
		{
			block.transactions.ToList().ForEach(tx =>
			{
				var txHash = Merkle.transactionHasher.Invoke(tx);
				_MempoolTxs[txHash] = new Tuple<Types.Transaction, bool>(tx, confirmed);
			});
		}
	}
}
