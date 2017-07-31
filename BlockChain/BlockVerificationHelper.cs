using Store;
using BlockChain.Store;
using Consensus;
using System;
using System.Linq;
using System.Collections.Generic;
using BlockChain.Data;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Collections;

namespace BlockChain
{
	using UtxoLookup = FSharpFunc<Types.Outpoint, FSharpOption<Types.Output>>;

	public class BlockVerificationHelper
    {
        public BkResult Result { get; set; }
        public readonly HashDictionary<TransactionValidation.PointedTransaction> ConfirmedTxs;
        public readonly HashDictionary<Types.Transaction> UnconfirmedTxs;
        public readonly List<QueueAction> QueueActions;

        UtxoLookup UtxoLookup;

		public enum BkResultEnum
		{
			Accepted,
			AcceptedOrphan,
			Rejected
		}

        //TODO: refactor using C# 7.0 and sub-classing each case
		public class BkResult
        {
            public BkResultEnum BkResultEnum { get; set; }
            public byte[] MissingParent { get; set; }

            public BkResult(BkResultEnum bkResultEnum) {
                BkResultEnum = bkResultEnum;
            }

            public BkResult(BkResultEnum bkResultEnum, byte[] missingParent) : this(bkResultEnum)
			{
				MissingParent = missingParent;
			}
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
			UnconfirmedTxs = invalidatedTxs ?? new HashDictionary<Types.Transaction>(); //todo: refactor to set as new obj by default
			QueueActions = queuedActions ?? new List<QueueAction>();

			_BlockChain = blockChain;
			_DbTx = dbTx;
			_BkHash = bkHash;
			_Bk = bk;

			if (!IsValid())
			{
				BlockChainTrace.Information($"block {_Bk.header.blockNumber} is invalid", _Bk);
                Result = new BkResult(BkResultEnum.Rejected);
				return;
			}

			if (IsInStore())
			{
				var reject = false;
                byte[] missingParent = null;

				switch (blockChain.BlockStore.GetLocation(dbTx, bkHash))
				{
					case LocationEnum.Branch:
						reject = !handleBranch;
						break;
					case LocationEnum.Orphans:
						missingParent = GetMissingParent();
						
						reject = !handleOrphan && !handleBranch;
						break;
					default:
						reject = true;
						break;
				}

				if (reject)
				{
                    Result = new BkResult(BkResultEnum.Rejected, missingParent);
					return;
				}
			}

			if (bk.transactions.Count() == 0)
			{
				BlockChainTrace.Information("empty tx list", bk);
				Result = new BkResult(BkResultEnum.Rejected);
				return;
			}

			if (!IsValidTime())
			{
				BlockChainTrace.Information("invalid time", bk);
				Result = new BkResult(BkResultEnum.Rejected);
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
                    BlockChainTrace.Information("invalid genesis block", bk);
					Result = new BkResult(BkResultEnum.Rejected);
					return;
				}
				else
				{
					blockChain.Timestamps.Init(bk.header.timestamp);
					ExtendMain(QueueActions, 0, true);
					Result = new BkResult(BkResultEnum.Accepted);
					BlockChainTrace.Information("accepted genesis block", bk);
					return;
				}
			}

			if (IsOrphan())
			{
                var missingParent = GetMissingParent();
                blockChain.BlockStore.Put(dbTx, bkHash, bk, LocationEnum.Orphans, 0);
                BlockChainTrace.Information($"block {_Bk.header.blockNumber} added as orphan", bk);
                Result = new BkResult(BkResultEnum.AcceptedOrphan, missingParent);
				return;
			}

			//12. Check that nBits value matches the difficulty rules

			if (!IsValidDifficulty() || !IsValidBlockNumber() || !IsValidTimeStamp())
			{
				BlockChainTrace.Information($"block {_Bk.header.blockNumber} rejected", bk);
				Result = new BkResult(BkResultEnum.Rejected);
				return;
			}

			//14. For certain old blocks(i.e.on initial block download) check that hash matches known values

			var totalWork = TotalWork();

            UtxoLookup = _BlockChain.UtxoLookupFactory(_DbTx, true);


			if (handleBranch) // make a branch block main
            {
                if (!ExtendMain(QueueActions, totalWork))
                {
                    Result = new BkResult(BkResultEnum.Rejected);
                    return;
                }
            }
            else if (!IsNewGreatestWork(totalWork))
            {
                blockChain.BlockStore.Put(dbTx, bkHash, bk, LocationEnum.Branch, totalWork);
            }
            else if (blockChain.BlockStore.IsLocation(dbTx, bk.header.parent, LocationEnum.Main))
            {
                if (!ExtendMain(QueueActions, totalWork))
                {
                    BlockChainTrace.Information($"block {_Bk.header.blockNumber} rejected", bk);
                    Result = new BkResult(BkResultEnum.Rejected);
                    return;
                }
            }
            else
            {
				BlockChainTrace.Information($"block {bk.header.blockNumber} extends a branch with new difficulty", bk);

                Reorg();
			}

            BlockChainTrace.Information($"block {bk.header.blockNumber} accepted", bk);
            Result = new BkResult(BkResultEnum.Accepted);
		}

		bool ExtendMain(List<QueueAction> queuedActions, double totalWork, bool isGenesis = false)
		{
            if (_BlockChain.BlockStore.ContainsKey(_DbTx, _BkHash))
            {
            	_BlockChain.BlockStore.SetLocation(_DbTx, _BkHash, LocationEnum.Main);
            }
            else
            {
            	_BlockChain.BlockStore.Put(_DbTx, _BkHash, _Bk, LocationEnum.Main, totalWork);
            }

			_BlockChain.Timestamps.Push(_Bk.header.timestamp);

			if (_Bk.header.blockNumber % 2000 == 0)
			{
				_BlockChain.BlockNumberDifficulties.Add(_DbTx.Transaction, _Bk.header.blockNumber, _BkHash);
			}

			var blockUndoData = new BlockUndoData();

			var confirmedTxs = new HashDictionary<TransactionValidation.PointedTransaction>();

            //TODO: lock with mempool
            for (var txIdx = 0; txIdx < _Bk.transactions.Count(); txIdx++)
			{
                var tx = _Bk.transactions[txIdx];
				var txHash = Merkle.transactionHasher.Invoke(tx);
				TransactionValidation.PointedTransaction ptx;

                if (!isGenesis)
                {
                    if ((txIdx == 0 && !IsCoinbaseTxValid(tx)) || (txIdx > 0 && IsCoinbaseTxValid(tx)))
                    {
                        return false;
                    }

                    if (!IsTransactionValid(tx, txHash, out ptx))
					{
						return false;
					}

					confirmedTxs[txHash] = ptx;

					BlockChainTrace.Information("saved tx", ptx);

                    foreach (var pInput in ptx.pInputs)
					{
						_BlockChain.UTXOStore.Remove(_DbTx, pInput.Item1);
						BlockChainTrace.Information($"utxo spent, amount {pInput.Item2.spend.amount}", ptx);
						BlockChainTrace.Information($" of", pInput.Item1.txHash);
						blockUndoData.RemovedUTXO.Add(new Tuple<Types.Outpoint, Types.Output>(pInput.Item1, pInput.Item2));
					}
                } 
                else
                {
                    ptx = TransactionValidation.toPointedTransaction(
                        tx,
                        ListModule.Empty<Types.Output>()
                    );
                }

                _BlockChain.BlockStore.TxStore.Put(_DbTx, txHash, tx, true);

				var contractExtendSacrifices = new HashDictionary<ulong>();
                var activationSacrifice = 0UL;

                for (var outputIdx = 0; outputIdx < tx.outputs.Count(); outputIdx++)
				{
                    var output = tx.outputs[outputIdx];

					if (output.@lock.IsContractSacrificeLock)
					{
						if (!output.spend.asset.SequenceEqual(Tests.zhash))
							continue; // not Zen

                        var contractSacrificeLock = (Types.OutputLock.ContractSacrificeLock)output.@lock;

						if (contractSacrificeLock.IsHighVLock)
							continue; // not current version

						if (contractSacrificeLock.Item.lockData.Length > 0 && contractSacrificeLock.Item.lockData[0] != null && contractSacrificeLock.Item.lockData[0].Length > 0)
						{
							var contractKey = contractSacrificeLock.Item.lockData[0]; // output-lock-level indicated contract

							contractExtendSacrifices[contractKey] =
								(contractExtendSacrifices.ContainsKey(contractKey) ? contractExtendSacrifices[contractKey] : 0) +
								output.spend.amount;
						}
                        else if (contractSacrificeLock.Item.lockData.Length == 0)
						{
                            activationSacrifice += output.spend.amount;
						}
					}

                    //todo: fix  to exclude CSLocks&FLocks, instead of including by locktype
					if (output.@lock.IsPKLock || output.@lock.IsContractLock)
					{
                        BlockChainTrace.Information($"new utxo, amount {output.spend.amount}", tx);
                        var outpoint = new Types.Outpoint(txHash, (uint)outputIdx);
                        _BlockChain.UTXOStore.Put(_DbTx, outpoint, output);
						blockUndoData.AddedUTXO.Add(new Tuple<Types.Outpoint, Types.Output>(outpoint, output));
					}
				}

                if (FSharpOption<Types.ExtendedContract>.get_IsSome(tx.contract) && !tx.contract.Value.IsHighVContract)
                {
                    var codeBytes = ((Types.ExtendedContract.Contract)tx.contract.Value).Item.code;
                    var contractHash = Merkle.innerHash(codeBytes);
                    var contractCode = System.Text.Encoding.ASCII.GetString(codeBytes);

                    if (_BlockChain.ActiveContractSet.TryActivate(_DbTx, contractCode, activationSacrifice, contractHash, _Bk.header.blockNumber))
					{
                        blockUndoData.ACSDeltas.Add(contractHash, new ACSUndoData());
						ContractsTxsStore.Add(_DbTx.Transaction, contractHash, txHash);
					}
                }

				foreach (var item in contractExtendSacrifices)
				{
                    var currentACSItem = _BlockChain.ActiveContractSet.Get(_DbTx, item.Key);

                    if (currentACSItem.Value != null)
                    {
                        if (_BlockChain.ActiveContractSet.TryExtend(_DbTx, item.Key, item.Value))
                        {
                            if (!blockUndoData.ACSDeltas.ContainsKey(item.Key))
                                blockUndoData.ACSDeltas.Add(item.Key, new ACSUndoData() { LastBlock = currentACSItem.Value.LastBlock });
                        }
                    }
				}
			}

			var expiringContracts = _BlockChain.ActiveContractSet.GetExpiringList(_DbTx, _Bk.header.blockNumber);

			foreach (var acsItem in expiringContracts)
			{
                if (!blockUndoData.ACSDeltas.ContainsKey(acsItem.Key))
                    blockUndoData.ACSDeltas.Add(acsItem.Key, new ACSUndoData() { ACSItem = acsItem.Value });
			}

            if (!isGenesis)
            {
				_BlockChain.BlockStore.SetUndoData(_DbTx, _BkHash, blockUndoData);
            }

			_BlockChain.ActiveContractSet.DeactivateContracts(_DbTx, expiringContracts.Select(t=>t.Key));

			ValidateACS();

			_BlockChain.ChainTip.Context(_DbTx).Value = _BkHash;
			//TODO: only update after commit
			_BlockChain.Tip = new Keyed<Types.Block>(_BkHash, _Bk);

            queuedActions.Add(new MessageAction(new BlockMessage(confirmedTxs, _Bk.header.blockNumber)));

            foreach (var item in confirmedTxs)
            {
                ConfirmedTxs[item.Key] = item.Value;
                UnconfirmedTxs.Remove(item.Key);
            }

			return true;
		}

        void Reorg()
        {
			var originalTip = _BlockChain.Tip;

			Keyed<Types.Block> fork = null;
			var newMainChain = GetNewMainChainStartFromForkToLeaf(new Keyed<Types.Block>(_BkHash, _Bk), out fork);

			_BlockChain.ChainTip.Context(_DbTx).Value = fork.Key;
			_BlockChain.Tip = fork;
			_BlockChain.InitBlockTimestamps(_DbTx);

			var oldMainChain = GetOldMainChainStartFromLeafToFork(fork, originalTip.Key);

			foreach (var block in oldMainChain)
			{
				UndoBlock(block.Value, block.Key);
			}

			//append new chain
			foreach (var _bk in newMainChain)
			{
				var action = new BlockVerificationHelper(
					_BlockChain,
					_DbTx,
					_bk.Key,
					_bk.Value,
					false,
					true,
					ConfirmedTxs,
					UnconfirmedTxs,
					QueueActions);

				BlockChainTrace.Information($"new main chain bk {_bk.Value.header.blockNumber} {action.Result.BkResultEnum}", _bk.Value);

				if (action.Result.BkResultEnum == BkResultEnum.Rejected)
				{
					_BlockChain.ChainTip.Context(_DbTx).Value = originalTip.Key;
					_BlockChain.Tip = originalTip;
					_BlockChain.InitBlockTimestamps(_DbTx);

					BlockChainTrace.Information("reorganization undo", _bk.Value);
					Result = new BkResult(BkResultEnum.Rejected);
					return;
				}
			}
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
            return !_BlockChain.BlockStore.ContainsKey(_DbTx, _Bk.header.parent) || 
               _BlockChain.BlockStore.IsLocation(_DbTx, _Bk.header.parent, LocationEnum.Orphans);
		}

        byte[] GetMissingParent()
        {
            var blockItr = _Bk.header.parent;

            while (true)
            {
                var block = _BlockChain.BlockStore.Get(_DbTx, blockItr);

                if (block == null)
                {
                    return blockItr;
                }

				blockItr = block.Value.BlockHeader.parent;
			}
		}

		bool IsInStore()
		{
			return _BlockChain.BlockStore.ContainsKey(_DbTx, _BkHash);
		}

        // remove
		bool IsTransactionValid(Types.Transaction tx, byte[] txHash, out TransactionValidation.PointedTransaction ptx)
		{
            if (_BlockChain.BlockStore.TxStore.ContainsKey(_DbTx, txHash) && _BlockChain.BlockStore.TxStore.Get(_DbTx, txHash).Value.InMainChain)
			{
				BlockChainTrace.Information("Tx already in store", txHash);
                ptx = null;
                return false;
			}

			switch (_BlockChain.IsOrphanTx(_DbTx, tx, true, out ptx))
			{
				case BlockChain.IsTxOrphanResult.Orphan:
					BlockChainTrace.Information("tx invalid - orphan", tx);
					return false;
				case BlockChain.IsTxOrphanResult.Invalid:
					BlockChainTrace.Information("tx invalid - reference(s)", tx);
					return false;
			}

            if (_BlockChain.IsDoubleSpend(_DbTx, tx, true))
                return false;

            //TODO: coinbase validation + check that witness has blocknumber

            if (!BlockChain.IsValidUserGeneratedTx(_DbTx, ptx))
			{
				BlockChainTrace.Information("tx invalid - structural", ptx);
                return false;
			}

			byte[] contractHash;
			switch (BlockChain.IsContractGeneratedTx(ptx, out contractHash))
			{
				case BlockChain.IsContractGeneratedTxResult.ContractGenerated:
					if (!_BlockChain.ActiveContractSet.IsActive(_DbTx, contractHash))
					{
						BlockChainTrace.Information("tx invalid - contract not active", tx);
						return false;
					}
                    var contractFunction = _BlockChain.ActiveContractSet.GetContractFunction(_DbTx, contractHash);

					if (!BlockChain.IsValidAutoTx(ptx, UtxoLookup, contractHash, contractFunction))
					{
						BlockChainTrace.Information("auto-tx invalid", ptx);
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
			var result = _Bk.header.timestamp >= _BlockChain.Timestamps.Median();

			if (!result)
                BlockChainTrace.Information($"block {_Bk.header.blockNumber} has invalid timestamp", _BkHash);
			
			return result;
		}

		bool IsValidDifficulty()
		{
            //TODO:

            //			UInt32 expectedDifficulty;

            //			if (_Bk.header.blockNumber % 2000 == 1)
            //			{
            //				var lastBlockHash = _BlockChain.BlockNumberDifficulties.GetLast(_DbTx.Transaction);
            //				var lastBlock = GetBlock(lastBlockHash);

            //                //
            //				var tip = _BlockChain.ChainTip.Context(_DbTx).Value;
            //				var tipBlock = GetBlock(tip).Value;
            //                //

            //                expectedDifficulty = tipBlock.header.pdiff;
            //                    //TODO: fix: update difficuly
            //                    //NewDifficulty(lastBlock.Value.header.timestamp, _Bk.header.timestamp);
            //			}
            //			else
            //			{
            //				var tip = _BlockChain.ChainTip.Context(_DbTx).Value;
            //				var tipBlock = GetBlock(tip).Value;
            //				expectedDifficulty = tipBlock.header.pdiff;
            //			}

            //			var blockDifficulty = _Bk.header.pdiff;

            //#if TRACE
            //			if (blockDifficulty != expectedDifficulty)
            //			{
            //                BlockChainTrace.Information($"block {_Bk.header.blockNumber}: expecting difficulty {expectedDifficulty}, found {blockDifficulty}", _BkHash);
            //			}
            //#endif

            //return blockDifficulty == expectedDifficulty;

            return true;
		}

        bool IsCoinbaseTxValid(Types.Transaction tx)
        {
            //TODO: check maturity

            return tx.witnesses.Count()> 0 && tx.witnesses[0].Length > 0 &&
                 BitConverter.ToUInt32(tx.witnesses[0], 0) == _Bk.header.blockNumber;
        }

		bool IsValidBlockNumber()
		{
			var parentBlock = GetBlock(_Bk.header.parent).Value;

#if TRACE
			if (parentBlock.header.blockNumber >= _Bk.header.blockNumber)
			{
				BlockChainTrace.Information($"block {_Bk.header.blockNumber}: expecting block-number greater than {parentBlock.header.blockNumber}, found {_Bk.header.blockNumber}", _BkHash);
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

		Keyed<Types.Block> GetBlock(byte[] bkHash)
		{
			return _BlockChain.BlockStore.GetBlock(_DbTx, bkHash);
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
            BlockChainTrace.Information($"undoing block {block.header.blockNumber}", block);

			_BlockChain.BlockStore.SetLocation(_DbTx, key, LocationEnum.Branch);

			var blockUndoData = _BlockChain.BlockStore.GetUndoData(_DbTx, key);

			if (blockUndoData != null)
			{
				blockUndoData.AddedUTXO.ForEach(u =>
				{
                    BlockChainTrace.Information($"undo block {block.header.blockNumber}: utxo removed, amount {u.Item2.spend.amount}", block);
					_BlockChain.UTXOStore.Remove(_DbTx, u.Item1.txHash, u.Item1.index);
				});

				blockUndoData.RemovedUTXO.ForEach(u =>
				{
					BlockChainTrace.Information($"undo block {block.header.blockNumber}: new utxo, amount {u.Item2.spend.amount}", block);
					_BlockChain.UTXOStore.Put(_DbTx, u.Item1.txHash, u.Item1.index, u.Item2);
				});

				foreach (var item in blockUndoData.ACSDeltas)
				{
                    if (item.Value.LastBlock.HasValue) // restore last block - undo extend
                    {
                        var current = new ActiveContractSet().Get(_DbTx, item.Key);

						if (current != null)
						{
                            current.Value.LastBlock = item.Value.LastBlock.Value;
                            _BlockChain.ActiveContractSet.Add(_DbTx, current.Value);
						}
                        else
                        {
                            BlockChainTrace.Error("missing ACS item!", new Exception());
                        }
                    }
                    else if (item.Value.ACSItem != null) // restore entire item - undo expire
					{
                        _BlockChain.ActiveContractSet.Add(_DbTx, item.Value.ACSItem);
                    }
                    else // remove item - undo activate
                    {
                        _BlockChain.ActiveContractSet.Remove(_DbTx, item.Key);
                    }
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
