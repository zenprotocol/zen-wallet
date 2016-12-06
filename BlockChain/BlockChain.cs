using System;
using Consensus;
using System.Linq;
using BlockChain.Store;
using Store;
using Infrastructure;
using System.Collections.Generic;
using Microsoft.FSharp.Collections;

namespace BlockChain
{
	public class BlockChain : ResourceOwner
	{
		private readonly TxMempool _TxMempool;
		private readonly TxStore _TxStore;
		private readonly BlockStore _MainBlockStore;
		private readonly BlockStore _BranchBlockStore;
		private readonly BlockStore _OrphanBlockStore;
		private readonly DBContext _DBContext;
		private readonly BlockDifficultyTable _BlockDifficultyTable;

		public event Action<Types.Transaction> OnAddedToMempool;

		public BlockChain(string dbName)
		{
			_DBContext = new DBContext(dbName);
			_TxMempool = new TxMempool();
			_TxStore = new TxStore();
			_MainBlockStore = new MainBlockStore();
			_BranchBlockStore = new BranchBlockStore();
			_OrphanBlockStore = new OrphanBlockStore();
			_BlockDifficultyTable = new BlockDifficultyTable();

			OwnResource(_DBContext);
			OwnResource(MessageProducer<TxMempool.AddedMessage>.Instance.AddMessageListener(
				new EventLoopMessageListener<TxMempool.AddedMessage>(m =>
				{
					OnAddedToMempool(m.Transaction.Value);
				})
			));

			EnsureGenesisTransaction();
		}

		//temp
		public void EnsureGenesisTransaction()
		{
			Consensus.Types.Transaction genesis = GetGenesisTransaction();
			byte[] key = Merkle.transactionHasher.Invoke(genesis);

			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				if (!_TxStore.ContainsKey(context, key))
				{
					_TxStore.Put(context, new Keyed<Types.Transaction>(key, genesis));
					context.Commit ();

					BlockChainTrace.Error("New Genesis Transaction's Address is: " + BitConverter.ToString(key), null);
				}
				else {
					BlockChainTrace.Error("Existing Genesis Transaction's Address is: " + BitConverter.ToString(key), null);
				}
			}
		}

		//temp
		private Types.Transaction GetGenesisTransaction()
		{
			var outputs = new List<Types.Output>();

			outputs.Add(new Types.Output(Consensus.Tests.cbaselock, new Types.Spend(Consensus.Tests.zhash, 1000)));

			var inputs = new List<Types.Outpoint>();

			var hashes = new List<byte[]>();

			//hack Concensus into giving a different hash per each tx created
			var version = (uint)1;

			Types.Transaction transaction = new Types.Transaction(version,
				ListModule.OfSeq(inputs),
				ListModule.OfSeq(hashes),
				ListModule.OfSeq(outputs),
				null);

			return transaction;
		}

		public bool HandleNewBlock(Types.Block block) //TODO: use Keyed type
		{
			var _block = new Keyed<Types.Block>(Merkle.blockHasher.Invoke(block), block);

			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				//var result = new BlockChainAddBlockOperation(
				//	context,
				//	new Keyed<Types.Block>(Merkle.blockHasher.Invoke(block), block),
				//	_TxMempool
				//).Start();

				//return result;

				//if (_MainBlockStore.ContainsKey(context, _block.Key) ||
				//	_BranchBlockStore.ContainsKey(context, _block.Key) ||
				//	_OrphanBlockStore.ContainsKey(context, _block.Key))
				//{
				//	return HandleNewBlockResult.Rejected;
				//}

				//var parent = _block.Value.header.parent;

				//if (!_MainBlockStore.ContainsKey(context, parent) &&
				//	!_BranchBlockStore.ContainsKey(context, parent))
				//{
				//	_OrphanBlockStore.Put(context, _block);
				//	context.Commit();
				//	return HandleNewBlockResult.AddedOrpan;
				//}
				//else 
				//{
				//	_MainBlockStore.Put(context, _block);
				//	//_TxStore.Put(context, block.transactions.ToArray());
				//	////TODO: fix that. difficulty is not computed recursively
				//	//_BlockDifficultyTable.Context(context)[key] = GetDifficultyRecursive(context, block);

				//	context.Commit();

				//	return HandleNewBlockResult.Accepeted;
				//}
			}

			return false;
		}

		public bool HandleNewTransaction(Types.Transaction transaction) //TODO: use Keyed type
		{
			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				var result = new BlockChainAddTransactionOperation(
					context,
					new Keyed<Types.Transaction>(Merkle.transactionHasher.Invoke(transaction), transaction),
					_TxMempool
				).Start();

				return result != BlockChainAddTransactionOperation.Result.Rejected;
				//switch (result)
				//{
				//	case BlockChainAddTransactionOperation.Result.Added:
				//		break;
				//	case BlockChainAddTransactionOperation.Result.AddedOrphaned:
				//		break;
				//	case BlockChainAddTransactionOperation.Result.Rejected:
				//		break;
				//}
			}
		}

		public Types.Transaction GetTransaction(byte[] key) //TODO: make concurrent
		{
			if (_TxMempool.ContainsKey(key))
			{
				return _TxMempool.Get(key).Value;
			}
			else {
				return null;
			}
		}


		//private Double GetDifficultyRecursive(TransactionContext context, Types.Block block)
		//{
		//	Double result = block.header.pdiff;

		//	if (block.header.parent == null || block.header.parent.Length == 0)
		//	{
		//		return result;
		//	}

		//	Types.Block parentBlock = _BlockStore.Get(context, block.header.parent).Value;

		//	if (parentBlock == null)
		//	{
		//		throw new Exception("Missing parent block");
		//	}

		//	return result + GetDifficultyRecursive(context, parentBlock);
		//}
	}
}
