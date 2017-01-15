using System;
using Consensus;
using BlockChain.Store;
using Store;
using Infrastructure;
using System.Text;
using System.Collections.Generic;
using Microsoft.FSharp.Collections;
using System.Linq;

namespace BlockChain
{
	public class BlockChain : ResourceOwner
	{
		private readonly TimeSpan OLD_TIP_TIME_SPAN = TimeSpan.FromMinutes(5);
		public TxMempool TxMempool { get; private set; }
		public TxStore TxStore { get; private set; }
		private readonly UTXOStore _UTXOStore;
		private readonly MainBlockStore _MainBlockStore;
		private readonly BranchBlockStore _BranchBlockStore;
		private readonly OrphanBlockStore _OrphanBlockStore;
	//	private readonly BlockStore _GenesisBlockStore;
		private readonly DBContext _DBContext;
		private readonly BlockDifficultyTable _BlockDifficultyTable;
		private readonly ChainTip _ChainTip;
		private readonly byte[] _GenesisBlockHash;

//		public event Action<Types.Transaction> OnAddedToMempool;
//		public event Action<Types.Transaction> OnAddedToStore;

		public bool IsTipOld //TODO: consider caching
		{
			get
			{
				var tipBlock = Tip;

				if (tipBlock == null)
				{
					return true;
				}
				else 
				{
					DateTime tipDateTime = DateTime.FromBinary(tipBlock.Value.header.timestamp);
					TimeSpan diff = DateTime.Now - tipDateTime;

					return diff > OLD_TIP_TIME_SPAN;
				}
			}
		}

		public Keyed<Types.Block> Tip { get; private set; }

		private Keyed<Types.Block> GetTip()
		{
			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				var chainTip = _ChainTip.Context(context).Value;

				return chainTip == null ? null : _MainBlockStore.Get(context, chainTip);
			}
		}

		public List<Types.Transaction> GetTransactions()
		{
			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				return TxStore.All(context).Select(t=>t.Value).ToList();
			}
		}

		public BlockChain(string dbName, byte[] genesisBlockHash) {
			_DBContext = new DBContext(dbName);
			TxMempool = new TxMempool();
			TxStore = new TxStore();
			_UTXOStore = new UTXOStore();
			_MainBlockStore = new MainBlockStore();
			_BranchBlockStore = new BranchBlockStore();
			_OrphanBlockStore = new OrphanBlockStore();
			_BlockDifficultyTable = new BlockDifficultyTable();
			_ChainTip = new ChainTip();
			_GenesisBlockHash = genesisBlockHash;// GetGenesisBlock().Key;

			OwnResource(_DBContext);
			//OwnResource(MessageProducer<TxMempool.AddedMessage>.Instance.AddMessageListener(
			//	new EventLoopMessageListener<TxMempool.AddedMessage>(m =>
			//	{
			//		if (OnAddedToMempool != null)
			//			OnAddedToMempool(m.Transaction.Value);
			//	})
			//));
			//OwnResource(MessageProducer<TxStore.AddedMessage>.Instance.AddMessageListener(
			//	new EventLoopMessageListener<TxStore.AddedMessage>(m =>
			//	{
			//		if (OnAddedToStore != null)
			//			OnAddedToStore(m.Transaction.Value);
			//	})
			//));

			Tip = GetTip();

			if (Tip != null)
			{
				//var currentBlockChain = new Stack<Types.Block>();

				var itr = Tip == null ? null : Tip.Value;

				int blocks = 0;

				while (itr != null)
				{
				//	currentBlockChain.Push(itr);
					itr = GetBlock(itr.header.parent);
					blocks++;
				}

				BlockChainTrace.Information("blocks: " + blocks);

				//BlockChainTrace.Information("Genesis is:\n" + System.Convert.ToBase64String(_GenesisBlockHash));

				//var sb = new StringBuilder();
				//sb.AppendLine("Current chain:");

				//while (currentBlockChain.Count != 0)
				//{
				//	itr = currentBlockChain.Pop();
				//	sb.AppendLine(System.Convert.ToBase64String(Merkle.blockHeaderHasher.Invoke(itr.header)));
				//}

				//BlockChainTrace.Information(sb.ToString());
			}
		}

		public BlockChainAddBlockOperation.Result HandleNewBlock(Types.Block block) //TODO: use Keyed type
		{
			//var _block = new Keyed<Types.Block>(Merkle.blockHasher.Invoke(block), block);
			var _block = new Keyed<Types.Block>(Merkle.blockHeaderHasher.Invoke(block.header), block);

			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				var result = new BlockChainAddBlockOperation(
					context,
					_block,
					_MainBlockStore,
					_BranchBlockStore,
					_OrphanBlockStore,
				//	_GenesisBlockStore,
					TxMempool,
					TxStore,
					_UTXOStore,
					_ChainTip,
					_GenesisBlockHash
				).Start();

				context.Commit();

				//TODO: only do that if tip has changed
				Tip = GetTip();

				BlockChainTrace.Information("Block " + System.Convert.ToBase64String(Merkle.blockHeaderHasher.Invoke(block.header)) + " is " + result);

				return result;
			}
		}

		public bool HandleNewTransaction(Types.Transaction transaction) //TODO: use Keyed type
		{
			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				var result = new BlockChainAddTransactionOperation(
					context,
					new Keyed<Types.Transaction>(Merkle.transactionHasher.Invoke(transaction), transaction),
					TxMempool,
					TxStore,
					_UTXOStore
				).Start();

				context.Commit();

				return result != BlockChainAddTransactionOperation.Result.Rejected;
				//switch (result)
				//{
				//	case BlockChainAddTransactionOperation.Result.Added:
				//		break;
				//	case BlockChainAddTransactionOperation.Result.AddedOrphan:
				//		break;
				//	case BlockChainAddTransactionOperation.Result.Rejected:
				//		break;
				//}
			}
		}

		public Types.Transaction GetTransaction(byte[] key) //TODO: make concurrent
		{
			if (TxMempool.ContainsKey(key))
			{
				return TxMempool.Get(key).Value;
			}
			else
			{
				using (TransactionContext context = _DBContext.GetTransactionContext())
				{
					if (TxStore.ContainsKey(context, key))
					{
						return TxStore.Get(context, key).Value;
					}
				}
			}

			return null;
		}

		public Types.Block GetBlock(byte[] key)
		{
			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				var bk = _MainBlockStore.Get(context, key);

				return bk == null ? null : bk.Value;
			}
		}

		public List<Tuple<Types.Outpoint,Types.Output>> GetUTXOSet()
		{
			var values = new List<Tuple<Types.Outpoint, Types.Output>>();

			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				foreach (var item in _UTXOStore.All(context))
				{
					byte[] txHash = new byte[item.Key.Length - 1];
					Array.Copy(item.Key, txHash, txHash.Length);

					uint index = item.Key[item.Key.Length - 1];

					var outpoint = new Types.Outpoint(txHash, index);

					values.Add(new Tuple<Types.Outpoint, Types.Output>(outpoint, item.Value));
				}
			}

			return values;
		}

		//demo
		public Types.Block MineAllInMempool()
		{
			var transactions = TxMempool.GetAll();

			if (transactions.Count == 0)
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
				new byte[] { },
				new byte[] { },
				new byte[] { },
				ListModule.OfSeq<byte[]>(new List<byte[]>()),
				DateTime.Parse(date).ToBinary(),
				1,
				nonce
			);

			var newBlock = new Types.Block(blockHeader, ListModule.OfSeq<Types.Transaction>(transactions.Select(t => t.Value)));

			if (HandleNewBlock(newBlock) == BlockChainAddBlockOperation.Result.Added)
			{
				return newBlock;
			}
			else 
			{
				throw new Exception();
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
