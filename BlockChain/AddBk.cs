using Store;
using BlockChain.Store;
using Consensus;
using System;
using System.Linq;
using System.Collections.Generic;
using BlockChain.Data;
using Microsoft.FSharp.Collections;

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

					var genesisPointedTransactions = new List<TransactionValidation.PointedTransaction>();

					foreach (var tx in _Bk.transactions)
					{
						genesisPointedTransactions.Add(TransactionValidation.toPointedTransaction(
							tx,
							ListModule.OfSeq<Types.Output>(new List<Types.Output>())
						));
					}

					ExtendMain(queuedActions, 0, genesisPointedTransactions);
					Result = ResultEnum.Added;
					return;
				}
			}

			if (IsOrphan())
			{
				blockChain.BlockStore.Put(dbTx, new Keyed<Types.Block>(bkHash, bk), LocationEnum.Orphans, 0);
				BlockChainTrace.Information("added as orphan");
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

			var pointedTransactions = new List<TransactionValidation.PointedTransaction>();

			if (!IsTransactionsValid(pointedTransactions))
			{
				Result = ResultEnum.Rejected;
				return;
			}

			if (handleBranch) // make a branch block main
			{
				ExtendMain(queuedActions, totalWork, pointedTransactions);
			}
			else if (IsBranch())
			{
				if (IsNewGreatestWork(totalWork))
				{


					BlockChainTrace.Information($"block {bk.header.blockNumber} extends a branch with new difficulty.");

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
						BlockChainTrace.Information("Start with sidechain for block: " + _bk.Value.header.blockNumber);

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
				ExtendMain(queuedActions, totalWork, pointedTransactions);
			}

			Result = ResultEnum.Added;
		}

		void ExtendMain(List<QueueAction> queuedActions, double totalWork, List<TransactionValidation.PointedTransaction> pointedTransactions)
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

			foreach (var transaction in _Bk.transactions)
			{
				var txHash = Merkle.transactionHasher.Invoke(transaction);

				BlockChainTrace.Information("new txstore item");

				_BlockChain.BlockStore.TxStore.Put(_DbTx, new Keyed<Types.Transaction>(txHash, transaction));

				for (int i = 0; i < transaction.inputs.Length; i++)
				{
					var outpoint = new Types.Outpoint(transaction.inputs[i].txHash, transaction.inputs[i].index);
					var output = _BlockChain.UTXOStore.Get(
						_DbTx,
						GetOutputKey(transaction.inputs[i].txHash, (int)transaction.inputs[i].index)
					);

					blockUndoData.RemovedUTXO.Add(new Tuple<Types.Outpoint, Types.Output>(outpoint, output.Value));
				}

				for (int i = 0; i < transaction.outputs.Length; i++)
				{
					BlockChainTrace.Information($"new utxo item: {transaction.outputs[i].spend.amount}");

					blockUndoData.AddedUTXO.Add(new Tuple<Types.Outpoint, Types.Output>(new Types.Outpoint(txHash, (uint)i), transaction.outputs[i]));
				}

				//	check for mempool: orphans becoming non
			}

			//	RemoveTransactionsFromMempool(bk);

			blockUndoData.AddedUTXO.ForEach(u =>
			{
				_BlockChain.UTXOStore.Put(_DbTx, new Keyed<Types.Output>(GetOutputKey(u.Item1.txHash, (int)u.Item1.index), u.Item2));
			});

			blockUndoData.RemovedUTXO.ForEach(u =>
			{
				_BlockChain.UTXOStore.Remove(_DbTx, GetOutputKey(u.Item1.txHash, (int)u.Item1.index));
			});

			_BlockChain.BlockStore.SetUndoData(_DbTx, _BkHash, blockUndoData);

			_BlockChain.ChainTip.Context(_DbTx).Value = _BkHash;
			_BlockChain.Tip = new Keyed<Types.Block>(_BkHash, _Bk);

			MarkMempoolTxs(_Bk, true);

			queuedActions.Add(new MessageAction(new NewBlockMessage(pointedTransactions, true)));
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

		bool IsTransactionsValid(List<TransactionValidation.PointedTransaction> pointedTransactions)
		{
			foreach (var tx in _Bk.transactions)
			{
				TransactionValidation.PointedTransaction ptx;

				if (_BlockChain.IsOrphanTx(_DbTx, tx, out ptx))
					return false;

				if (!_BlockChain.IsValidTransaction(_DbTx, ptx))
					return false;

				pointedTransactions.Add(ptx);
			}

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

			while (!itr.Key.SequenceEqual(fork.Key))
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
