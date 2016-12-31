using Store;
using BlockChain.Store;
using Consensus;
using System;
using System.Linq;

namespace BlockChain
{
	public class BlockChainAddBlockOperation
	{
		private readonly TransactionContext _TransactionContext;
		private readonly MainBlockStore _MainBlockStore;
		private readonly BranchBlockStore _BranchBlockStore;
		private readonly OrphanBlockStore _OrphanBlockStore;
		private readonly BlockStore _GenesisBlockStore;
		private readonly TxMempool _TxMempool;
		private readonly TxStore _TxStore;
		private readonly UTXOStore _UTXOStore;
		private readonly ChainTip _ChainTip;
		private readonly Keyed<Types.Block> _NewBlock; //TODO: or just use var key
		private readonly byte[] _GenesisBlockHash;

		private PlacementEnum _Placement;

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
			Rejected
		}

		public BlockChainAddBlockOperation(
			TransactionContext transactionContext,
		   	Keyed<Types.Block> block,
			MainBlockStore mainBlockStore,
			BranchBlockStore branchBlockStore,
			OrphanBlockStore orphanBlockStore,
	//		BlockStore genesisBlockStore,
			TxMempool txMempool,
			TxStore txStore,
			UTXOStore utxoStore,
			ChainTip chainTip,
			byte[] genesisBlockHash
		)
		{
			_TransactionContext = transactionContext;
			_NewBlock = block;
			_MainBlockStore = mainBlockStore;
			_BranchBlockStore = branchBlockStore;
			_OrphanBlockStore = orphanBlockStore;
		//	_GenesisBlockStore = genesisBlockStore;
			_TxMempool = txMempool;
			_TxStore = txStore;
			_UTXOStore = utxoStore;
			_ChainTip = chainTip;
			_GenesisBlockHash = genesisBlockHash;
		}

		public Result Start(bool IsOrphan = false)
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

			if (!IsOrphan && IsInOrpandStore())
			{
				BlockChainTrace.Information("block already in store");
				return Result.Rejected;
			}

			//TODO:
			/*
			3. Transaction list must be non - empty
			4. Block hash must satisfy claimed nBits proof of work
			5. Block timestamp must not be more than two hours in the future
			6. First transaction must be coinbase(i.e.only 1 input, with hash = 0, n = -1), the rest must not be
			7. For each transaction, apply "tx" checks 2 - 4
			8. For the coinbase(first) transaction, scriptSig length must be 2 - 100
			9. Reject if sum of transaction sig opcounts > MAX_BLOCK_SIGOPS
			10. Verify Merkle hash
			*/

			DeterminPlacement();

			if (_Placement == PlacementEnum.Orphans)
			{
				_OrphanBlockStore.Put(_TransactionContext, _NewBlock);
				BlockChainTrace.Information("added as orphan");
				return Result.AddedOrphan;
				//TODO: query peer we got this from for 1st missing orphan block in prev chain
			}

			//TODO:
			/*
			12. Check that nBits value matches the difficulty rules
			13. Reject if timestamp is the median time of the last 11 blocks or before
			14. For certain old blocks(i.e.on initial block download) check that hash matches known values
			*/

			switch (_Placement)
			{
				case PlacementEnum.Genesis:
					if (!IsGenesisValid(_NewBlock.Value))
					{
						return Result.Rejected;
					}
					AddToMainBlockStore();
					break;
				case PlacementEnum.Main:
					if (!IsTransactionsValid(_NewBlock.Value))
					{
						return Result.Rejected;
					}
					//Reject if coinbase value > sum of block creation fee and transaction fees

					//For each transaction, "Add to wallet if mine"
					AddToMainBlockStore();
					break;
					//Relay block to our peers
				//case PlacementEnum.BranchChangeOver:
				//	foreach (var block in PotentialNewChain())
				//	{
				//		//Do "branch" checks 3-11 ???????
				//		if (IsTransactionsValid(_NewBlock.Value))
				//		{
				//		}
				//	}
				//	break;
				case PlacementEnum.Branch:
					_BranchBlockStore.Put(_TransactionContext, _NewBlock);
					break;
			}

			if (IsOrphan)
			{
				_OrphanBlockStore.Remove(_TransactionContext, _NewBlock.Key);
			}

			foreach (var block in _OrphanBlockStore.GetOrphansOf(_TransactionContext, _NewBlock))
			{
				BlockChainTrace.Information("Start with orphan");
				new BlockChainAddBlockOperation(
					_TransactionContext,
					block,
					_MainBlockStore,
					_BranchBlockStore,
					_OrphanBlockStore,
					_TxMempool,
					_TxStore,
					_UTXOStore,
					_ChainTip,
					_GenesisBlockHash
				).Start(true);
			}
	
			return Result.Added;
		}

		private bool IsValid()
		{
			return true;
		}

		private void DeterminPlacement()
		{
			var parent = _NewBlock.Value.header.parent;

			if (_NewBlock.Key.SequenceEqual(_GenesisBlockHash))
			{
				_Placement = PlacementEnum.Genesis;
			} else if (_MainBlockStore.ContainsKey(_TransactionContext, parent))
			{
				_Placement = PlacementEnum.Main;
			}
			else if (_BranchBlockStore.ContainsKey(_TransactionContext, parent))
			{
				if (IsNewDifficulty())
				{
					_Placement = PlacementEnum.BranchChangeOver;
				}
				else
				{
					_Placement = PlacementEnum.Branch;
				}
			}
			else
			{
				_Placement = PlacementEnum.Orphans;
			}
		}

		private bool IsInMainOrBranchStore()
		{
			return
				_MainBlockStore.ContainsKey(_TransactionContext, _NewBlock.Key) ||
               	_BranchBlockStore.ContainsKey(_TransactionContext, _NewBlock.Key); //||
			//  	_OrphanBlockStore.ContainsKey(_TransactionContext, _NewBlock.Key);
		}

		private bool IsInOrpandStore()
		{
			return
			//	_MainBlockStore.ContainsKey(_TransactionContext, _NewBlock.Key) ||
			//	_BranchBlockStore.ContainsKey(_TransactionContext, _NewBlock.Key) ||
			  	_OrphanBlockStore.ContainsKey(_TransactionContext, _NewBlock.Key);
		}


		private bool IsTransactionsValid(Types.Block block)
		{
			foreach (var transaction in block.transactions)
			{
				//if (transaction. is coinbase)
				//{
				//	continue;
				//}

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
			if (!_TxStore.ContainsKey(_TransactionContext, input.txHash))
			{
				BlockChainTrace.Information("referenced transaction missing for input");
				return false;
			}

			var transaction = _TxStore.Get(_TransactionContext, input.txHash).Value;

			if (transaction.outputs.Length < input.index + 1)
			{
				BlockChainTrace.Information("referenced transaction has fewer inputs");
				return false;
			}

			return true;
		}

		private bool ParentOutputSpent(Types.Outpoint outpoint)
		{
			if (!_TxStore.ContainsKey(_TransactionContext, outpoint.txHash))
			{
				throw new Exception("Tx not found");
			}

			var transaction = _TxStore.Get(_TransactionContext, outpoint.txHash).Value;

			if (!_UTXOStore.ContainsKey(_TransactionContext, GetOutputKey(transaction, (int)outpoint.index)))
			{
				BlockChainTrace.Information("Output has been spent");
				return true;
			}

			return false;
		}

		private void AddToMainBlockStore() { 
			_MainBlockStore.Put(_TransactionContext, _NewBlock);
		//	_ChainTip.Context(_TransactionContext).Value = Consensus.Merkle.blockHasher.Invoke(_NewBlock.Value);
			_ChainTip.Context(_TransactionContext).Value = Consensus.Merkle.blockHeaderHasher.Invoke(_NewBlock.Value.header);
			StoreTransactions();
		}

		private void StoreTransactions()
		{
			foreach (var transaction in _NewBlock.Value.transactions)
			{
				var txHash = Merkle.transactionHasher.Invoke(transaction);

				_TxStore.Put(_TransactionContext, new Keyed<Types.Transaction>(txHash, transaction));

				if (_TxMempool.ContainsKey(txHash)) // fucntion adresses orphans also.
				{
					_TxMempool.Remove(txHash);
				}

				for (int i = 0; i < transaction.outputs.Length; i++)
				{
					_UTXOStore.Put(_TransactionContext, new Keyed<Types.Output>(GetOutputKey(transaction, i), transaction.outputs[i]));
				}
			}
		}

		private byte[] GetOutputKey(Types.Transaction transaction, int index)
		{
			var output = transaction.outputs[index];
			var txHash = Merkle.transactionHasher.Invoke(transaction);

			byte[] outputKey = new byte[txHash.Length + 1];
			txHash.CopyTo(outputKey, 1);
			outputKey[txHash.Length] = (byte)index;

			return outputKey;
		}

		//private void PotentialNewChain()
		//{
		//	var tip;
		//	var tipAncestors = new List<byte[]>();
		//	var brancAncestors = new List<byte[]>();

		//	byte[] tipCurrent;
		//	byte[] branchCurrent;
		//	byte[] fork = null;

		//	while (fork == null)
		//	{
		//	//	_NewBlock.Value.header.parent;

		//		if (tipAncestors.Contains(branchCurrent))
		//		{
		//			fork = branchCurrent;
		//		}
		//	    else if (brancAncestors.Contains(tipCurrent))
		//		{
		//			fork = tipCurrent;
		//		}
		//	}
		//}

		private bool IsNewDifficulty()
		{
			return false;
		}

		private bool IsGenesisValid(Types.Block block)
		{
			return true;
		}
	}
}
