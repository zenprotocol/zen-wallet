using Store;
using BlockChain.Store;
using Consensus;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace BlockChain
{
	public class AddBk
	{
		private readonly BlockChain _BlockChain;
		private readonly TransactionContext _DbTx;
		private readonly Keyed<Types.Block> _Bk;
		private readonly List<Action> _DoActions;
		private readonly List<Action> _UndoActions;
		private readonly ConcurrentStack<Types.Block> _OrphansActions;

		public enum Result
		{
			Added,
			AddedOrphan,
			Rejected
		}

		public AddBk(
			BlockChain blockChain,
			TransactionContext dbTx,
			Keyed<Types.Block> bk,
			List<Action> doActions,
			List<Action> undoActions,
			ConcurrentStack<Types.Block> handlingStack
		)
		{
			_DoActions = doActions;
			_UndoActions = undoActions;
			_BlockChain = blockChain;
			_Bk = bk;
			_DbTx = dbTx;
			_OrphansActions = handlingStack;
		}

		public Result Start(bool handleOrphan = false, bool handleBranch = false)
		{
			if (!IsValid())
			{
				BlockChainTrace.Information("not valid");
				return Result.Rejected;
			}

			if (IsInStore())
			{
				var reject = false;

				switch (_BlockChain.BlockStore.GetLocation(_DbTx, _Bk.Key))
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
					BlockChainTrace.Information("block already in store: " + _BlockChain.BlockStore.GetLocation(_DbTx, _Bk.Key));
					return Result.Rejected;
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
					return Result.Rejected;
				}
				else
				{
					_BlockChain.Timestamps.Init(_Bk.Value.header.timestamp);
					AddToMainBlockStore(0);
					_BlockChain.ChainTip.Context(_DbTx).Value = _Bk.Key;
					return Result.Added;
				}
			}

			if (IsOrphan())
			{
				_BlockChain.BlockStore.Put(_DbTx, _Bk, LocationEnum.Orphans, 0);
				BlockChainTrace.Information("added as orphan");
				return Result.AddedOrphan;
			}

			//12. Check that nBits value matches the difficulty rules

			if (!IsValidDifficulty() || !IsValidBlockNumber() || !IsValidTimeStamp())
			{
				return Result.Rejected;
			}

			if (!IsTransactionsValid(_Bk.Key))
			{
				return Result.Rejected;
			}

			//14. For certain old blocks(i.e.on initial block download) check that hash matches known values

			var totalWork = TotalWork();

			if (handleBranch)
			{
				AddToMainBlockStore(totalWork);
			}
			else if (IsBranch())
			{
				if (IsNewDifficulty(totalWork))
				{
					Keyed<Types.Block> fork = null;
					var newMainChain = GetNewMainChainStartFromForkToLeaf(_Bk, out fork);

					//append new chain
					foreach (var block in newMainChain)
					{
						BlockChainTrace.Information("Start with sidechain");
						var result = new AddBk(
							_BlockChain,
							_DbTx,
							block,
							_DoActions,
							_UndoActions,
							_OrphansActions
						).Start(false, true);

						if (result != Result.Added)
						{
							 _BlockChain.InitBlockTimestamps();

							return Result.Rejected;
						}
					}

					//TODO: handle do/undo at this point?

					// remove from main + add to mempool of old main
					foreach (var block in GetOldMainChainStartFromLeafToFork(fork))
					{
						RemoveFromMainBlockStore(block.Key);
					}

					//// remove from mempoo of new main
					//foreach (var block in newMainChain)
					//{
					//	RemoveTransactionsFromMempool(block);
					//}

					_BlockChain.ChainTip.Context(_DbTx).Value = _Bk.Key;
				}
				else
				{
					_BlockChain.BlockStore.Put(_DbTx, _Bk, LocationEnum.Branch, totalWork);
				}
			}
			else
			{
				_BlockChain.ChainTip.Context(_DbTx).Value = _Bk.Key;
				AddToMainBlockStore(totalWork);
			}

			foreach (var block in _BlockChain.BlockStore.Children(_DbTx, _Bk.Key, true))
			{
				BlockChainTrace.Information("Start with orphan");
				_OrphansActions.Push(block.Value);
			}

			return Result.Added;
		}

		private bool IsValid()
		{
			return true;
		}

		private bool IsCoinbase(Types.Transaction tx)
		{
			return false;
		}

		private bool IsGenesis()
		{
			return _Bk.Key.SequenceEqual(_BlockChain.GenesisBlockHash);
		}

		private bool IsOrphan()
		{
			return !_BlockChain.BlockStore.ContainsKey(_DbTx, _Bk.Value.header.parent);
		}

		private bool IsInStore()
		{
			return _BlockChain.BlockStore.ContainsKey(_DbTx, _Bk.Key);
		}

		private bool IsBranch()
		{
			return 
				_BlockChain.BlockStore.IsLocation(_DbTx, _Bk.Value.header.parent, LocationEnum.Branch) ||
				_BlockChain.BlockStore.HasChildren(_DbTx, _Bk.Value.header.parent);
		}

		private bool IsTransactionsValid(byte[] block)
		{
			foreach (var tx in _BlockChain.BlockStore.Transactions(_DbTx, block))
			{
				if (IsCoinbase(tx.Value))
				{
					continue;
				}

				foreach (Types.Outpoint input in tx.Value.inputs)
				{
					if (!ParentOutputExists(input))
					{
						BlockChainTrace.Information("parent output does not exist");
						return false;
					}

					//For each input, if the referenced output transaction is coinbase (i.e.only 1 input, with hash = 0, n = -1), it must have at least COINBASE_MATURITY (100) confirmations; else reject.
					//Verify crypto signatures for each input; reject if any are bad

					if (ParentOutputSpent(input))
					{
						BlockChainTrace.Information("parent output spent");
						return false;
					}

					//Using the referenced output transactions to get input values, check that each input value, as well as the sum, are in legal money range
					//Reject if the sum of input values < sum of output values
				}
			}

			return true;
		}

		private bool ParentOutputExists(Types.Outpoint input)
		{
			if (!_BlockChain.BlockStore.TxStore.ContainsKey(_DbTx, input.txHash))
			{
				BlockChainTrace.Information("referenced transaction missing for input");
				return false;
			}

			var transaction = _BlockChain.BlockStore.TxStore.Get(_DbTx, input.txHash).Value;

			if (transaction.outputs.Length < input.index + 1)
			{
				BlockChainTrace.Information("referenced transaction has fewer inputs");
				return false;
			}

			return true;
		}

		private bool ParentOutputSpent(Types.Outpoint outpoint)
		{
			if (!_BlockChain.BlockStore.TxStore.ContainsKey(_DbTx, outpoint.txHash))
			{
				throw new Exception("Tx not found");
			}

			var transaction = _BlockChain.BlockStore.TxStore.Get(_DbTx, outpoint.txHash).Value;

			if (!_BlockChain.UTXOStore.ContainsKey(_DbTx, GetOutputKey(transaction, (int)outpoint.index)))
			{
				BlockChainTrace.Information("Output has been spent");
				return true;
			}

			return false;
		}

		private bool IsValidTimeStamp()
		{
			var result = _Bk.Value.header.timestamp > _BlockChain.Timestamps.Median();

			if (!result)
				BlockChainTrace.Information("invalid timestamp");
			
			return result;
		}

		private bool IsValidDifficulty()
		{
			UInt32 expectedDifficulty;

			if (_Bk.Value.header.blockNumber % 2000 == 1)
			{
				var lastBlockHash = _BlockChain.BlockNumberDifficulties.GetLast(_DbTx.Transaction);
				var lastBlock = GetBlock(lastBlockHash);
				expectedDifficulty = NewDifficulty(lastBlock.Value.header.timestamp, _Bk.Value.header.timestamp);
			}
			else
			{
				var tip = _BlockChain.ChainTip.Context(_DbTx).Value;
				var tipBlock = GetBlock(tip).Value;
				expectedDifficulty = tipBlock.header.pdiff;
			}

			var blockDifficulty = _Bk.Value.header.pdiff;

#if TRACE
			if (blockDifficulty != expectedDifficulty)
			{
				BlockChainTrace.Information($"expecting difficulty {expectedDifficulty}, found {blockDifficulty}");
			}
#endif

			return blockDifficulty == expectedDifficulty;
		}

		private bool IsValidBlockNumber()
		{
			var parentBlock = GetBlock(_Bk.Value.header.parent).Value;

#if TRACE
			if (parentBlock.header.blockNumber >= _Bk.Value.header.blockNumber)
			{
				BlockChainTrace.Information($"expecting block-number greater than {parentBlock.header.blockNumber}, found {_Bk.Value.header.blockNumber}");
			}
#endif
			return parentBlock.header.blockNumber < _Bk.Value.header.blockNumber;
		}

		private UInt32 NewDifficulty(long startTime, long endTime)
		{
			return 0;
		}

		private void AddToMainBlockStore(double totalWork) {
			if (_BlockChain.BlockStore.ContainsKey(_DbTx, _Bk.Key))
			{
				_BlockChain.BlockStore.SetLocation(_DbTx, _Bk.Key, LocationEnum.Main);
			}
			else
			{
				_BlockChain.BlockStore.Put(_DbTx, _Bk, LocationEnum.Main, totalWork);
			}

			_BlockChain.Timestamps.Push(_Bk.Value.header.timestamp);

			if (_Bk.Value.header.blockNumber % 2000 == 0)
			{
				_BlockChain.BlockNumberDifficulties.Add(_DbTx.Transaction, _Bk.Value.header.blockNumber, _Bk.Key);
			}

			foreach (var transaction in _Bk.Value.transactions)
			{
				var txHash = Merkle.transactionHasher.Invoke(transaction);

				BlockChainTrace.Information("new txstore item");

				var keyedTx = new Keyed<Types.Transaction>(txHash, transaction);
				_BlockChain.BlockStore.TxStore.Put(_DbTx, keyedTx);

				if (_BlockChain.TxMempool.ContainsKey(txHash)) // function adresses orphans also.
				{
					_DoActions.Add(() => _BlockChain.TxMempool.Remove(txHash));
					_UndoActions.Add(() => _BlockChain.TxMempool.Add(txHash, _BlockChain.GetPointedTransaction(_DbTx, transaction)));
				}

				for (int i = 0; i < transaction.outputs.Length; i++)
				{
					BlockChainTrace.Information($"new utxo item: {transaction.outputs[i].spend.amount}");
					_BlockChain.UTXOStore.Put(_DbTx, new Keyed<Types.Output>(GetOutputKey(transaction, i), transaction.outputs[i]));
					_UndoActions.Add(() => _BlockChain.TxMempool.Add(txHash, _BlockChain.GetPointedTransaction(_DbTx, transaction)));
				}
			}

			_DoActions.Add(() => BkAddedMessage.Publish(_Bk.Value));
		}

		private byte[] GetOutputKey(Types.Transaction transaction, int index) //TODO: convert to outpoint
		{
			var output = transaction.outputs[index];
			var txHash = Merkle.transactionHasher.Invoke(transaction);

			byte[] outputKey = new byte[txHash.Length + 1];
			txHash.CopyTo(outputKey, 0);
			outputKey[txHash.Length] = (byte)index;

			return outputKey;
		}

		private double TotalWork()
		{
			var parentTotalWork = _BlockChain.BlockStore.TotalWork(_DbTx, _Bk.Value.header.parent);
			
			return TransactionValidation.totalWork(
				parentTotalWork,
				TransactionValidation.uncompressDifficulty(_Bk.Value.header.pdiff)
			);
		}

		private bool IsNewDifficulty(double totalWork)
		{
			var tip = _BlockChain.ChainTip.Context(_DbTx).Value;
			var tipBlock = GetBlock(tip);
			var tipTotalWork = _BlockChain.BlockStore.TotalWork(_DbTx, tipBlock.Key);

			return totalWork > tipTotalWork;
		}

		private bool IsGenesisValid()
		{
			return true;
		}

		private Keyed<Types.Block> GetBlock(byte[] bkHash)
		{
			return _BlockChain.BlockStore.GetBlock(_DbTx, bkHash);
		}

		private List<Keyed<Types.Block>> GetNewMainChainStartFromForkToLeaf(Keyed<Types.Block> leaf, out Keyed<Types.Block> fork)
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

        private List<Keyed<Types.Block>> GetOldMainChainStartFromLeafToFork(Keyed<Types.Block> fork)
		{
			var tip = _BlockChain.ChainTip.Context(_DbTx).Value;
			var itr = GetBlock(tip);
			var list = new List<Keyed<Types.Block>>();

			while (!itr.Key.SequenceEqual(fork.Key))
			{
				list.Add(itr);
				itr = GetBlock(itr.Value.header.parent);
			}

			return list;
		}

		private void RemoveFromMainBlockStore(byte[] block)
		{
			_BlockChain.BlockStore.SetLocation(_DbTx, block, LocationEnum.Branch);

			foreach (var tx in _BlockChain.BlockStore.Transactions(_DbTx, block))
			{
				_BlockChain.TxMempool.Add(tx.Key, _BlockChain.GetPointedTransaction(_DbTx, tx.Value));
				_UndoActions.Add(() => _BlockChain.TxMempool.Remove(tx.Key));

				for (var i = 0; i < tx.Value.outputs.Length; i++)
				{
					_BlockChain.UTXOStore.Remove(_DbTx, GetOutputKey(tx.Value, i));
					//_UndoActions.Add(() => _BlockChain.TxMempool.Add(tx));
				}
			}
		}

		private void RemoveTransactionsFromMempool(Keyed<Types.Block> block)
		{
			foreach (var tx in block.Value.transactions)
			{
				//TODO: use extension methods for hash calc?
				var txHash = Merkle.transactionHasher.Invoke(tx);

				if (_BlockChain.TxMempool.ContainsKey(txHash))
				{
					_BlockChain.TxMempool.Remove(txHash);
				}
			}
		}
	}
}
