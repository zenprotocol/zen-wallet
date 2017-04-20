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
		public BkResultEnum Result { get; set; }
		public readonly HashDictionary<TransactionValidation.PointedTransaction> ConfirmedTxs;
		public readonly HashDictionary<Types.Transaction> UnconfirmedTxs;
		public readonly List<QueueAction> QueuedActions;

		public enum BkResultEnum
		{
			Accepted,
			AcceptedOrphan,
			AcceptedSideChain,
			Rejected,
		}

		BlockChain _BlockChain;
		TransactionContext _DbTx;
		byte[] _BkHash;
		Types.Block _Bk;

		public BlockVerificationHelper(
			BlockChain blockChain,
			TransactionContext dbTx,
			byte[] bkHash,
			Types.Block bk,
			bool handleOrphan = false,
			bool handleBranch = false,
			HashDictionary<TransactionValidation.PointedTransaction> confirmedTxs = null,
			HashDictionary<Types.Transaction> invalidatedTxs = null,
			List<QueueAction> queuedActions = null
		)
		{
			ConfirmedTxs = confirmedTxs ?? new HashDictionary<TransactionValidation.PointedTransaction>();
			UnconfirmedTxs = invalidatedTxs ?? new HashDictionary<Types.Transaction>();
			QueuedActions = queuedActions ?? new List<QueueAction>();

			_BlockChain = blockChain;
			_DbTx = dbTx;
			_BkHash = bkHash;
			_Bk = bk;

			if (!IsValid())
			{
				BlockChainTrace.Information("not valid", bk);
				Result = BkResultEnum.Rejected;
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
					BlockChainTrace.Information("block already in store: " + blockChain.BlockStore.GetLocation(dbTx, bkHash), bkHash);
					Result = BkResultEnum.Rejected;
					return;
				}
			}

			if (bk.transactions.Count() == 0)
			{
				BlockChainTrace.Information("empty tx list", bkHash);
				Result = BkResultEnum.Rejected;
				return;
			}

			if (!IsValidTime())
			{
				BlockChainTrace.Information("invalid time", bkHash);
				Result = BkResultEnum.Rejected;
				return;
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
					BlockChainTrace.Information("invalid genesis block", _Bk);
					Result = BkResultEnum.Rejected;
					return;
				}
				else
				{
					blockChain.Timestamps.Init(bk.header.timestamp);
					ExtendMain(QueuedActions, 0);
					Result = BkResultEnum.Accepted;
					BlockChainTrace.Information("accepted genesis block", _Bk);
					return;
				}
			}

			if (IsOrphan())
			{
				blockChain.BlockStore.Put(dbTx, new Keyed<Types.Block>(bkHash, bk), LocationEnum.Orphans, 0);
				BlockChainTrace.Information("block added as orphan", _Bk);
				Result = BkResultEnum.AcceptedOrphan;
				return;
			}

			//12. Check that nBits value matches the difficulty rules

			if (!IsValidDifficulty() || !IsValidBlockNumber() || !IsValidTimeStamp())
			{
				BlockChainTrace.Information("block rejected", _Bk);
				Result = BkResultEnum.Rejected;
				return;
			}

			//14. For certain old blocks(i.e.on initial block download) check that hash matches known values

			var totalWork = TotalWork();

			if (handleBranch) // make a branch block main
			{
				if (!ExtendMain(QueuedActions, totalWork))
				{
					Result = BkResultEnum.Rejected;
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
					//	return;2
					//}

					BlockChainTrace.Information($"block {bk.header.blockNumber} extends a branch with new difficulty.", _Bk);

					var originalTip = blockChain.Tip;

					Keyed<Types.Block> fork = null;
					var newMainChain = GetNewMainChainStartFromForkToLeaf(new Keyed<Types.Block>(bkHash, bk), out fork);

					blockChain.ChainTip.Context(dbTx).Value = fork.Key;
					blockChain.Tip = fork;

					var oldMainChain = GetOldMainChainStartFromLeafToFork(fork, originalTip.Key);

					foreach (var block in oldMainChain)
					{
						UndoBlock(block.Value, block.Key);
					}

					//append new chain
					foreach (var _bk in newMainChain)
					{
						var action = new BlockVerificationHelper(
							blockChain,
							dbTx,
							_bk.Key,
							_bk.Value,
							false,
							true,
							ConfirmedTxs,
							UnconfirmedTxs,
							QueuedActions);
						
						BlockChainTrace.Information($"new main chain bk {_bk.Value.header.blockNumber} {action.Result}", _bk.Value);

						if (action.Result == BkResultEnum.Rejected)
						{
							blockChain.ChainTip.Context(dbTx).Value = originalTip.Key;
							blockChain.Tip = originalTip;
							blockChain.InitBlockTimestamps();

							BlockChainTrace.Information("undo reorganization", _bk.Value);
							Result = BkResultEnum.Rejected;
							return;
						}
					}
				}
				else
				{
					blockChain.BlockStore.Put(dbTx, new Keyed<Types.Block>(bkHash, bk), LocationEnum.Branch, totalWork);
					BlockChainTrace.Information("block accepted", _Bk);
					Result = BkResultEnum.AcceptedSideChain;
					return;
				}
			}
			else
			{
				if (!ExtendMain(QueuedActions, totalWork))
				{
					BlockChainTrace.Information("block rejected", _Bk);
					Result = BkResultEnum.Rejected;
					return;
				}
			}

			BlockChainTrace.Information("block accepted", _Bk);
			Result = BkResultEnum.Accepted;
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

			var confirmedTxs = new HashDictionary<TransactionValidation.PointedTransaction>();

			//TODO: lock with mempool
			foreach (var transaction in _Bk.transactions)
			{
				var txHash = Merkle.transactionHasher.Invoke(transaction);
				TransactionValidation.PointedTransaction ptx;

				if (!IsTransactionValid(transaction, txHash, out ptx))
				{
					return false;
				}

				confirmedTxs[txHash] = ptx;

				BlockChainTrace.Information("saved tx", ptx);

				_BlockChain.BlockStore.TxStore.Put(_DbTx, txHash, transaction);

				uint i = 0;
				foreach (var input in ptx.pInputs)
				{
					//TODO: refactoring is needed.
					_BlockChain.UTXOStore.Remove(_DbTx, input.Item1);
					BlockChainTrace.Information($"utxo spent, amount {input.Item2.spend.amount}", ptx);
					BlockChainTrace.Information($" of", input.Item1.txHash);
					blockUndoData.RemovedUTXO.Add(new Tuple<Types.Outpoint, Types.Output>(input.Item1, input.Item2));
					i++;
				}

				var contractSacrifices = new HashDictionary<ulong>();

				i = 0;
				foreach (var output in ptx.outputs)
				{
					// extending a contract?
					if (output.@lock.IsContractSacrificeLock)
					{
						if (!output.spend.asset.SequenceEqual(Tests.zhash))
							continue; // not Zen

						var contractLock = (Types.OutputLock.ContractSacrificeLock)output.@lock;

						if (contractLock.IsHighVLock)
							continue; // not current version

						byte[] contractKey = null;

						if (contractLock.Item.lockData[0] != null && contractLock.Item.lockData[0].Length > 0)
						{
							contractKey = contractLock.Item.lockData[0]; // output-lock-level indicated contract
						}
						else if (transaction.contract.Value != null)
						{
							if (transaction.contract.Value.IsHighVContract)
								continue; // not current version

							contractKey = ((Types.ExtendedContract.Contract)transaction.contract.Value).Item.code;
						}

						contractSacrifices[contractKey] =
							(contractSacrifices.ContainsKey(contractKey) ? contractSacrifices[contractKey] : 0) +
							output.spend.amount;
					}

					if (output.@lock.IsPKLock || output.@lock.IsContractLock)
					{
						BlockChainTrace.Information($"new utxo, amount {output.spend.amount}", ptx);
						_BlockChain.UTXOStore.Put(_DbTx, txHash, i, output);
						blockUndoData.AddedUTXO.Add(new Tuple<Types.Outpoint, Types.Output>(new Types.Outpoint(txHash, (uint)i), output));
					}
					i++;
				}

				foreach (var item in contractSacrifices)
				{
					if (new ActiveContractSet().IsActive(_DbTx, item.Key))
					{
						// snapshot only if first time (not snapshoted before)
						if (!blockUndoData.ACSDeltas.ContainsKey(item.Key))
						{
							blockUndoData.ACSDeltas.Add(item.Key,
								  new ActiveContractSet().Get(_DbTx, item.Key).Value);
						}

						new ActiveContractSet().Extend(_DbTx, item.Key, item.Value);
					}
					else
					{
						new ActiveContractSet().Activate(_DbTx, item.Key, item.Value);
					}
				}
			}

			var expiringContracts = new ActiveContractSet().GetExpiringList(_DbTx, _Bk.header.blockNumber);

			foreach (var acsItem in expiringContracts)
			{
				blockUndoData.ACSDeltas.Add(acsItem.Key, acsItem.Value);
			}

			_BlockChain.BlockStore.SetUndoData(_DbTx, _BkHash, blockUndoData);

			new ActiveContractSet().DeactivateContracts(_DbTx, expiringContracts.Select(t=>t.Key));

			ValidateACS();

			_BlockChain.ChainTip.Context(_DbTx).Value = _BkHash;
			_BlockChain.Tip = new Keyed<Types.Block>(_BkHash, _Bk);

			queuedActions.Add(new MessageAction(new BlockMessage(confirmedTxs, true)));

			foreach (var item in confirmedTxs)
				ConfirmedTxs[item.Key] = item.Value;

			return true;
		}

		bool IsValid()
		{
			return true;
		}

		bool IsValidTime()
		{
			var ts = new DateTime(_Bk.header.timestamp) - DateTime.Now.ToUniversalTime();

			return ts.Hours < 2;
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

		bool IsTransactionValid(Types.Transaction tx, byte[] txHash, out TransactionValidation.PointedTransaction ptx)
		{
			uint i = 0;
			foreach (var output in tx.outputs)
			{
				if (_BlockChain.UTXOStore.ContainsKey(_DbTx, txHash, i))
				{
					BlockChainTrace.Information("tx invalid - exists", tx);
					ptx = null;
					return false;
				}
			}

			switch (_BlockChain.IsOrphanTx(_DbTx, tx, out ptx))
			{
				case BlockChain.IsTxOrphanResult.Orphan:
					BlockChainTrace.Information("tx invalid - orphan", tx);
					return false;
				case BlockChain.IsTxOrphanResult.Invalid:
					BlockChainTrace.Information("tx invalid - reference(s)", tx);
					return false;
			}

			byte[] contractHash;
			switch (BlockChain.IsContractGeneratedTx(ptx, out contractHash))
			{
				case BlockChain.IsContractGeneratedTxResult.NotContractGenerated:
					if (!_BlockChain.IsValidTransaction(_DbTx, ptx))
					{
						BlockChainTrace.Information("tx invalid - universal", ptx);
						return false;
					}
					break;
				case BlockChain.IsContractGeneratedTxResult.ContractGenerated:
					if (!new ActiveContractSet().IsActive(_DbTx, contractHash))
					{
						BlockChainTrace.Information("tx invalid - contract not active", tx);
						return false;
					}
					if (!_BlockChain.IsValidTransaction(_DbTx, ptx))
					{
						BlockChainTrace.Information("tx invalid - universal", ptx);
						return false;
					}
					if (!BlockChain.IsContractGeneratedTransactionValid(_DbTx, ptx, contractHash))
					{
						BlockChainTrace.Information("tx invalid - invalid contract", ptx);
						return false;
					}
					break;
				case BlockChain.IsContractGeneratedTxResult.Invalid:
					BlockChainTrace.Information("tx invalid - input locks", tx);
					return false;
			}

			return true;
		}

		bool IsValidTimeStamp()
		{
			//TODO: assert block's timestamp isn't too far in the future
			var result = _Bk.header.timestamp > _BlockChain.Timestamps.Median();

			if (!result)
				BlockChainTrace.Information("invalid timestamp", _Bk);
			
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
				BlockChainTrace.Information($"expecting difficulty {expectedDifficulty}, found {blockDifficulty}", _Bk);
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
				BlockChainTrace.Information($"expecting block-number greater than {parentBlock.header.blockNumber}, found {_Bk.header.blockNumber}", _Bk);
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

		//byte[] GetOutputKey(Types.Transaction transaction, int index) //TODO: convert to outpoint
		//{
		//	return GetOutputKey(Merkle.transactionHasher.Invoke(transaction), index);
		//}

		//byte[] GetOutputKey(byte[] txHash, int index) //TODO: convert to outpoint
		//{
		//	byte[] outputKey = new byte[txHash.Length + 1];
		//	txHash.CopyTo(outputKey, 0);
		//	outputKey[txHash.Length] = (byte)index;

		//	return outputKey;
		//}

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
			BlockChainTrace.Information("block undo", block);

			_BlockChain.BlockStore.SetLocation(_DbTx, key, LocationEnum.Branch);

			var blockUndoData = _BlockChain.BlockStore.GetUndoData(_DbTx, key);

			if (blockUndoData != null)
			{
				blockUndoData.AddedUTXO.ForEach(u =>
				{
					BlockChainTrace.Information($"undo block: utxo removed, amount {u.Item2.spend.amount}", block);
					_BlockChain.UTXOStore.Remove(_DbTx, u.Item1.txHash, u.Item1.index);
				});

				blockUndoData.RemovedUTXO.ForEach(u =>
				{
					BlockChainTrace.Information($"undo block: new utxo, amount {u.Item2.spend.amount}", block);
					_BlockChain.UTXOStore.Put(_DbTx, u.Item1.txHash, u.Item1.index, u.Item2);
				});

				foreach (var item in blockUndoData.ACSDeltas)
				{
					new ActiveContractSet().Add(_DbTx, item.Value);
				}
			}

			block.transactions.ToList().ForEach(tx =>
			{
				var txHash = Merkle.transactionHasher.Invoke(tx);
				UnconfirmedTxs[txHash] = tx;
			});
		}
	}
}
