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
		private readonly BlockUndoData _BlockUndoData;

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
			_BlockUndoData = new BlockUndoData();
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

			//14. For certain old blocks(i.e.on initial block download) check that hash matches known values

			var totalWork = TotalWork();

			//var skipOrphans = false;

			if (handleBranch)
			{
				AddToMainBlockStore(totalWork);
			}
			else if (IsBranch())
			{
				if (IsNewGreatestWork(totalWork))
				{
					if (!IsTransactionsValid(true))
					{
						return Result.Rejected;
					}

					BlockChainTrace.Information($"block {_Bk.Value.header.blockNumber} extends a branch with new difficulty.");

					var originalTip = _BlockChain.Tip;

					Keyed<Types.Block> fork = null;
					var newMainChain = GetNewMainChainStartFromForkToLeaf(_Bk, out fork);

					_BlockChain.ChainTip.Context(_DbTx).Value = fork.Key;
					_BlockChain.Tip = fork;

					var oldMainChain = GetOldMainChainStartFromLeafToFork(fork, originalTip.Key);

					foreach (var block in oldMainChain)
					{
						//TODO: tx invalidation?
						UndoBlock(block.Key);
					}

					bool undoNewMainChain = false;

					//append new chain
					foreach (var block in newMainChain)
					{
						BlockChainTrace.Information("Start with sidechain for block: " + block.Value.header.blockNumber);

						if (new AddBk(
							_BlockChain,
							_DbTx,
							block,
							_DoActions,
							_UndoActions,
							_OrphansActions
						).Start(false, true) != Result.Added)
						{
							undoNewMainChain = true;
							break;
						}
					}

					if (undoNewMainChain)
					{
						_BlockChain.ChainTip.Context(_DbTx).Value = originalTip.Key;
						_BlockChain.Tip = originalTip;
						_BlockChain.InitBlockTimestamps();

						return Result.Rejected;
					}
					// remove from main + add to mempool of old main
					foreach (var block in oldMainChain)
					{
						BlockChainTrace.Information($"doin somethin with block {block.Value.header.blockNumber} {block.Value.transactions.Length} txs");
						//TODO
						block.Value.transactions.ToList().ForEach((Types.Transaction obj) =>
						{
							BlockChainTrace.Information($"tx invalidated of block {block.Value.header.blockNumber}");
							TxInvalidatedMessage.Publish(_BlockChain.GetPointedTransaction(_DbTx, obj));

								//TODO: undo
							});

						EvictIntoMempool(block.Key);
					}

					//// remove from mempoo of new main
					foreach (var block in newMainChain)
					{
						RemoveTransactionsFromMempool(block);
					}
				}
				else
				{
					_BlockChain.BlockStore.Put(_DbTx, _Bk, LocationEnum.Branch, totalWork);
				}
			}
			else
			{
				AddToMainBlockStore(totalWork);
			}

			//if (!skipOrphans)
			foreach (var block in _BlockChain.BlockStore.Children(_DbTx, _Bk.Key, true))
			{
				_OrphansActions.Push(block.Value);
			}

			return Result.Added;
		}

		void EvictIntoMempool(byte[] block)
		{
			foreach (var tx in _BlockChain.BlockStore.Transactions(_DbTx, block))
			{
				if (IsTransactionValid(tx.Value, false))
				{
					_DoActions.Add(() => _BlockChain.TxMempool.Add(tx.Key, _BlockChain.GetPointedTransaction(_DbTx, tx.Value)));
					_UndoActions.Add(() => _BlockChain.TxMempool.Remove(tx.Key));
				}
			}
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

		private bool IsTransactionsValid(bool checkExistance)
		{
			foreach (var tx in _Bk.Value.transactions)
			{
				if (!IsTransactionValid(tx, checkExistance))
				{
					return false;
				}
			}

			return true;
		}
							
		private bool IsTransactionValid(Types.Transaction tx, bool checkExistance)
		{
			if (IsCoinbase(tx))
			{
				return true;
			}

			//TODO: checkExistance?
			if (checkExistance)
			{
				var key = Merkle.transactionHasher.Invoke(tx);
				if (_BlockChain.BlockStore.TxStore.ContainsKey(_DbTx, key))
					return false;
			}

			foreach (Types.Outpoint input in tx.inputs)
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

				var toEvict = _BlockChain.TxMempool.GetTransactionsInConflict(transaction).ToList();

				_DoActions.Add(() =>
				{
					toEvict.ForEach(i => _BlockChain.TxMempool.Remove(i.Item1));
				});

				_UndoActions.Add(() =>
				{
					toEvict.ForEach(i => _BlockChain.TxMempool.Add(i.Item1, i.Item2));
				});

				for (int i = 0; i < transaction.inputs.Length; i++)
				{
					var outpoint = new Types.Outpoint(transaction.inputs[i].txHash, transaction.inputs[i].index);
					var output = _BlockChain.UTXOStore.Get(
						_DbTx, 
						GetOutputKey(transaction.inputs[i].txHash, (int)transaction.inputs[i].index)
					);

					_BlockUndoData.RemovedUTXO.Add(new Tuple<Types.Outpoint, Types.Output>(outpoint, output.Value));
					//_BlockChain.UTXOStore.Remove(
					//	_DbTx, 
					//	GetOutputKey(transaction.inputs[i].txHash, (int)transaction.inputs[i].index)
					//);
				}

				for (int i = 0; i < transaction.outputs.Length; i++)
				{
					BlockChainTrace.Information($"new utxo item: {transaction.outputs[i].spend.amount}");

					_BlockUndoData.AddedUTXO.Add(new Tuple<Types.Outpoint, Types.Output>(new Types.Outpoint(txHash, (uint)i), transaction.outputs[i]));
					//_BlockChain.UTXOStore.Put(_DbTx, new Keyed<Types.Output>(GetOutputKey(transaction, i), transaction.outputs[i]));

					_UndoActions.Add(() => _BlockChain.TxMempool.Add(txHash, _BlockChain.GetPointedTransaction(_DbTx, transaction)));
				}
			}

			RemoveTransactionsFromMempool(_Bk);

			_BlockUndoData.AddedUTXO.ForEach(u =>                    
			{
				_BlockChain.UTXOStore.Put(_DbTx, new Keyed<Types.Output>(GetOutputKey(u.Item1.txHash, (int)u.Item1.index), u.Item2));
			});

			_BlockUndoData.RemovedUTXO.ForEach(u =>
			{
				_BlockChain.UTXOStore.Remove(_DbTx, GetOutputKey(u.Item1.txHash, (int)u.Item1.index));
			});

			_BlockChain.BlockStore.SetUndoData(_DbTx, _Bk.Key, _BlockUndoData);

			_BlockChain.ChainTip.Context(_DbTx).Value = _Bk.Key;
			_BlockChain.Tip = _Bk;

			_DoActions.Add(() => BkAddedMessage.Publish(_Bk.Value));
		}

		private byte[] GetOutputKey(Types.Transaction transaction, int index) //TODO: convert to outpoint
		{
			return GetOutputKey(Merkle.transactionHasher.Invoke(transaction), index);
		}

		private byte[] GetOutputKey(byte[] txHash, int index) //TODO: convert to outpoint
		{
			byte[] outputKey = new byte[txHash.Length + 1];
			txHash.CopyTo(outputKey, 0);
			outputKey[txHash.Length] = (byte)index;

			return outputKey;
		}

		private double TotalWork()
		{
			return 1000 * _Bk.Value.header.blockNumber;
			//var parentTotalWork = _BlockChain.BlockStore.TotalWork(_DbTx, _Bk.Value.header.parent);
			
			//return TransactionValidation.totalWork(
			//	parentTotalWork,
			//	_Bk.Value.header.pdiff
			//);
		}

		private bool IsNewGreatestWork(double totalWork)
		{
			var tip = _BlockChain.ChainTip.Context(_DbTx).Value;
			var tipBlock = GetBlock(tip);
			var tipTotalWork = 1000 * tipBlock.Value.header.blockNumber; // _BlockChain.BlockStore.TotalWork(_DbTx, tipBlock.Key);

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

		private List<Keyed<Types.Block>> GetOldMainChainStartFromLeafToFork(Keyed<Types.Block> fork, byte[] tip)
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

		private void UndoBlock(byte[] block)
		{
			_BlockChain.BlockStore.SetLocation(_DbTx, block, LocationEnum.Branch);

			//undo utxos
			foreach (var tx in _BlockChain.BlockStore.Transactions(_DbTx, block))
			{
				for (var i = 0; i < tx.Value.outputs.Length; i++)
				{
					var blockUndoData = _BlockChain.BlockStore.GetUndoData(_DbTx, block);

					blockUndoData.AddedUTXO.ForEach(u =>
					{
						_BlockChain.UTXOStore.Remove(_DbTx, GetOutputKey(u.Item1.txHash, (int)u.Item1.index));
					});

					blockUndoData.RemovedUTXO.ForEach(u =>
					{
						_BlockChain.UTXOStore.Put(_DbTx, new Keyed<Types.Output>(GetOutputKey(u.Item1.txHash, (int)u.Item1.index), u.Item2));
					});




					//_BlockChain.UTXOStore.Remove(_DbTx, GetOutputKey(tx.Value, i));
					////_UndoActions.Add(() => _BlockChain.TxMempool.Add(tx));

					//var outpoint = new Types.Outpoint(tx.Value.inputs[i].txHash, tx.Value.inputs[i].index);
					//var output = _BlockChain.UTXOStore.Get(
					//	_DbTx,
					//	GetOutputKey(tx.Value.inputs[i].txHash, (int)tx.Value.inputs[i].index)
					//);

					//_BlockUndoData.RemovedUTXO.Add(new Tuple<Types.Outpoint, Types.Output>(outpoint, output.Value));
					////_BlockChain.UTXOStore.Remove(
					////	_DbTx, 
					////	GetOutputKey(transaction.inputs[i].txHash, (int)transaction.inputs[i].index)
					////);
				}
			}

			//foreach (var tx in _BlockChain.BlockStore.Transactions(_DbTx, block))
			//{
			//	_BlockChain.TxMempool.Add(tx.Key, _BlockChain.GetPointedTransaction(_DbTx, tx.Value));
			//	_UndoActions.Add(() => _BlockChain.TxMempool.Remove(tx.Key));
			//}
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
					//TODO: undo needed?
					//_UndoActions.Add(() => _BlockChain.TxMempool.Add(txHash, TransactionValidation.toPointedTransaction( tx));
				}
			}
		}
	}
}
