using Store;
using BlockChain.Store;
using Consensus;
using System;
using System.Linq;
using BlockChain.Data;
using System.Collections.Generic;
using NUnit.Framework;
using Microsoft.FSharp.Collections;
using Infrastructure;

namespace BlockChain
{
	public class AddBk
	{
		private readonly BlockChain _BlockChain;
		private readonly TransactionContext _DbTx;
		private readonly Keyed<Types.Block> _Bk; //TODO: or just use var key
		private readonly List<Action> _DoActions;
		private readonly List<Action> _UndoActions;

		private enum PlacementEnum
		{
			Genesis,
			Main,
			Branch,
			BranchChangeOver,
			Orphans
		}

		public enum Result
		{
			Added,
			AddedOrphan,
			Rejected,
			ChangeOverRejected
		}

		public AddBk(
			BlockChain blockChain,
			TransactionContext dbTx,
		   	Keyed<Types.Block> bk,
			List<Action> doActions,
			List<Action> undoActions
		)
		{
			_DoActions = doActions;
			_UndoActions = undoActions;
			_BlockChain = blockChain;
			_Bk = bk;
			_DbTx = dbTx;
		}

		public Result Start(bool IsOrphan = false, bool IsSideChain = false)
		{
			if (!IsValid())
			{
				BlockChainTrace.Information("not valid");
				return Result.Rejected;
			}

			if (IsInMainOrBranchStore())
			{
				BlockChainTrace.Information("block already in main/branch store");
				return Result.Rejected;
			}

			if (!IsOrphan && IsInOrphanStore())
			{
				BlockChainTrace.Information("block already in store");
				return Result.Rejected;
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

			var placement = DeterminePlacement();

			if (placement == PlacementEnum.Orphans)
			{
				_BlockChain.OrphanBlockStore.Put(_DbTx, _Bk);
				BlockChainTrace.Information("added as orphan");
				return Result.AddedOrphan;
				//TODO: query peer we got this from for 1st missing orphan block in prev chain
			}

			//12. Check that nBits value matches the difficulty rules

			if (placement != PlacementEnum.Genesis)
			{
				if (!IsValidDifficulty())
				{
					BlockChainTrace.Information("invalid difficulty");
					return Result.Rejected;
				}

				if (!IsValidTimeStamp())
				{
					BlockChainTrace.Information("invalid timestamp");
					return Result.Rejected;
				}
			}
			//14. For certain old blocks(i.e.on initial block download) check that hash matches known values

			switch (placement)
			{
				case PlacementEnum.Genesis:
					if (!IsGenesisValid(_Bk.Value))
					{
						return Result.Rejected;
					}
					_BlockChain.Timestamps.Init(_Bk.Value.header.timestamp);
					AddToMainBlockStore();
					break;
				case PlacementEnum.Main:
					if (!IsTransactionsValid(_Bk.Value))
					{
						return Result.Rejected;
					}
					//Reject if coinbase value > sum of block creation fee and transaction fees

					//For each transaction, "Add to wallet if mine"
					AddToMainBlockStore();
					break;
					//Relay block to our peers
				case PlacementEnum.BranchChangeOver:
					foreach (var block in Utils.BlocksList(
						_Bk, 
						_BlockChain.ChainTip.Context(_DbTx).Value,
						GetBlock
						))
					{
						BlockChainTrace.Information("Start with sidechain");
						var result = new AddBk(
							_BlockChain,
							_DbTx,
							block,
							_DoActions,
							_UndoActions
						).Start(false, true);

						if (result != Result.Added)
						{
							return Result.ChangeOverRejected;
						}
					}
					break;
				case PlacementEnum.Branch:
					_BlockChain.BranchBlockStore.Put(_DbTx, _Bk);
					break;
			}

			if (IsOrphan)
			{
				_BlockChain.OrphanBlockStore.Remove(_DbTx, _Bk.Key);
			}

			if (IsSideChain)
			{
				_BlockChain.BranchBlockStore.Remove(_DbTx, _Bk.Key);
			}

			foreach (var block in _BlockChain.OrphanBlockStore.OrphansOf(_DbTx, _Bk))
			{
				BlockChainTrace.Information("Start with orphan");
				new AddBk(
					_BlockChain,
					_DbTx,
					block,
					_DoActions,
					_UndoActions
				).Start(true);
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

		private PlacementEnum DeterminePlacement()
		{
			PlacementEnum placement;

			var parent = _Bk.Value.header.parent;

			if (_Bk.Key.SequenceEqual(_BlockChain.GenesisBlockHash))
			{
				placement = PlacementEnum.Genesis;
			} else if (_BlockChain.MainBlockStore.ContainsKey(_DbTx, parent))
			{
				placement = PlacementEnum.Main;
			}
			else if (_BlockChain.BranchBlockStore.ContainsKey(_DbTx, parent))
			{
				if (IsNewDifficulty())
				{
					placement = PlacementEnum.BranchChangeOver;
				}
				else
				{
					placement = PlacementEnum.Branch;
				}
			}
			else
			{
				placement = PlacementEnum.Orphans;
			}

			return placement;
		}

		private bool IsInMainOrBranchStore()
		{
			return
				_BlockChain.MainBlockStore.ContainsKey(_DbTx, _Bk.Key) ||
               	_BlockChain.BranchBlockStore.ContainsKey(_DbTx, _Bk.Key);
		}

		private bool IsInOrphanStore()
		{
			return _BlockChain.OrphanBlockStore.ContainsKey(_DbTx, _Bk.Key);
		}

		private bool IsTransactionsValid(Types.Block block)
		{
			foreach (var transaction in block.transactions)
			{
				if (IsCoinbase(transaction))
				{
					continue;
				}

				foreach (Types.Outpoint input in transaction.inputs)
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
			if (!_BlockChain.TxStore.ContainsKey(_DbTx, input.txHash))
			{
				BlockChainTrace.Information("referenced transaction missing for input");
				return false;
			}

			var transaction = _BlockChain.TxStore.Get(_DbTx, input.txHash).Value;

			if (transaction.outputs.Length < input.index + 1)
			{
				BlockChainTrace.Information("referenced transaction has fewer inputs");
				return false;
			}

			return true;
		}

		private bool ParentOutputSpent(Types.Outpoint outpoint)
		{
			if (!_BlockChain.TxStore.ContainsKey(_DbTx, outpoint.txHash))
			{
				throw new Exception("Tx not found");
			}

			var transaction = _BlockChain.TxStore.Get(_DbTx, outpoint.txHash).Value;

			if (!_BlockChain.UTXOStore.ContainsKey(_DbTx, GetOutputKey(transaction, (int)outpoint.index)))
			{
				BlockChainTrace.Information("Output has been spent");
				return true;
			}

			return false;
		}

		private bool IsValidTimeStamp()
		{
			return _Bk.Value.header.timestamp > _BlockChain.Timestamps.Median();
		}

		private bool IsValidDifficulty()
		{
			if (_Bk.Value.header.blockNumber % 2000 == 1)
			{
				var lastBlockHash = _BlockChain.BlockDifficultyTable.GetLast(_DbTx.Transaction);
				var lastBlock = GetBlock(lastBlockHash);

				return _Bk.Value.header.pdiff == NewDifficulty(lastBlock.Value.header.timestamp, _Bk.Value.header.timestamp);
			}

			var tip = _BlockChain.ChainTip.Context(_DbTx).Value;
			var tipBlock = GetBlock(tip).Value;
			var currentDifficulty = tipBlock.header.pdiff;

			return currentDifficulty == _Bk.Value.header.pdiff;
		}

		private void AddToMainBlockStore() { 
			_BlockChain.MainBlockStore.Put(_DbTx, _Bk);
			_BlockChain.ChainTip.Context(_DbTx).Value = Merkle.blockHeaderHasher.Invoke(_Bk.Value.header);
			_BlockChain.Timestamps.Push(_Bk.Value.header.timestamp);

			if (_Bk.Value.header.blockNumber % 2000 == 0)
			{
				_BlockChain.BlockDifficultyTable.Add(_DbTx.Transaction, _Bk.Value.header.blockNumber, _Bk.Key);
			}

			StoreTransactions();
		}

		private UInt32 NewDifficulty(long startTime, long endTime)
		{
			return 1;
		}

		private void StoreTransactions()
		{
			foreach (var transaction in _Bk.Value.transactions)
			{
				var txHash = Merkle.transactionHasher.Invoke(transaction);

				BlockChainTrace.Information("new txstore item");

				var keyedTx = new Keyed<Types.Transaction>(txHash, transaction);
				_BlockChain.TxStore.Put(_DbTx, keyedTx);
				_DoActions.Add(() => TxAddedMessage.Publish(keyedTx, true));

				if (_BlockChain.TxMempool.ContainsKey(txHash)) // fucntion adresses orphans also.
				{
					_BlockChain.TxMempool.Remove(txHash);
					_UndoActions.Add(() => _BlockChain.TxMempool.Add(keyedTx));
				}

				for (int i = 0; i < transaction.outputs.Length; i++)
				{
					BlockChainTrace.Information($"new utxo item: {transaction.outputs[i].spend.amount}");
					_BlockChain.UTXOStore.Put(_DbTx, new Keyed<Types.Output>(GetOutputKey(transaction, i), transaction.outputs[i]));
					_UndoActions.Add(() => _BlockChain.TxMempool.Add(keyedTx));
				}
			}
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

		private bool IsNewDifficulty()
		{
			return false;
		}

		private bool IsGenesisValid(Types.Block block)
		{
			return true;
		}

		private Keyed<Types.Block> GetBlock(byte[] bkHash)
		{
			return _BlockChain.MainBlockStore.Get(_DbTx, bkHash);
		}
	}
}
