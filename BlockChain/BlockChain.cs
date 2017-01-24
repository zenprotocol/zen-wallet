using System;
using Consensus;
using BlockChain.Store;
using Store;
using Infrastructure;
using System.Text;
using System.Collections.Generic;
using Microsoft.FSharp.Collections;
using System.Linq;
using BlockChain.Data;
using NUnit.Framework;
using System.IO;

namespace BlockChain
{
	public class TxAddedMessage
	{
		public Keyed<Types.Transaction> Tx { get; set; }
		public bool IsConfirmed { get; set; }

		public static void Publish(Keyed<Types.Transaction> tx, bool isConfirmed)
		{
			MessageProducer<TxAddedMessage>.Instance.PushMessage(new TxAddedMessage()
			{
				Tx = tx,
				IsConfirmed = isConfirmed
			});
		}
	}

	public class BkAddedMessage
	{
		public Keyed<Types.Block> Bk { get; set; }

		public static void Publish(Keyed<Types.Block> bk)
		{
			MessageProducer<BkAddedMessage>.Instance.PushMessage(new BkAddedMessage()
			{
				Bk = bk
			});
		}
	}

	public class BlockChain : ResourceOwner
	{
		private readonly TimeSpan OLD_TIP_TIME_SPAN = TimeSpan.FromMinutes(5);
		public TxMempool TxMempool { get; private set; }
		public UTXOStore UTXOStore { get; private set; }
		public TxStore TxStore { get; private set; }
		public MainBlockStore MainBlockStore { get; private set; }
		public BranchBlockStore BranchBlockStore { get; private set; }
		public OrphanBlockStore OrphanBlockStore { get; private set; }
		public ContractStore ContractStore { get; private set; }
 		private readonly DBContext _DBContext;
		public BlockDifficultyTable BlockDifficultyTable { get; private set; }
		public ChainTip ChainTip { get; private set; }
		public BlockTimestamps Timestamps { get; private set; }
		public byte[] GenesisBlockHash { get; private set; }

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
				var chainTip = ChainTip.Context(context).Value;

				return chainTip == null ? null : MainBlockStore.Get(context, chainTip);
			}
		}

		//public IEnumerable<Keyed<Types.Transaction>> GetTransactions()
		//{
		//	using (TransactionContext context = _DBContext.GetTransactionContext())
		//	{
		//		return TxStore.All(context);//.ToList();
		//	}
		//}

		public BlockChain(string dbName, byte[] genesisBlockHash) {
			_DBContext = new DBContext(dbName);
			TxMempool = new TxMempool();
			TxStore = new TxStore();
			UTXOStore = new UTXOStore();
			MainBlockStore = new MainBlockStore();
			BranchBlockStore = new BranchBlockStore();
			OrphanBlockStore = new OrphanBlockStore();
			ContractStore = new ContractStore();
			BlockDifficultyTable = new BlockDifficultyTable();
			ChainTip = new ChainTip();
			Timestamps = new BlockTimestamps();
			GenesisBlockHash = genesisBlockHash;// GetGenesisBlock().Key;

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

			InitBlockTimestamps();
		}

		void InitBlockTimestamps()
		{
			if (Tip != null)
			{
				var timestamps = new List<long>();
				var itr = Tip == null ? null : Tip.Value;

				while (itr != null && timestamps.Count < BlockTimestamps.SIZE)
				{
					timestamps.Add(itr.header.timestamp);
					itr = GetBlock(itr.header.parent);
				}
				Timestamps.Init(timestamps.ToArray());
			}
		}

		public AddBk.Result HandleNewBlock(Types.Block block) //TODO: use Keyed type
		{
			var doActions = new List<Action>();
			var undoActions = new List<Action>();

			//var _block = new Keyed<Types.Block>(Merkle.blockHasher.Invoke(block), block);
			var _block = new Keyed<Types.Block>(Merkle.blockHeaderHasher.Invoke(block.header), block);

			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				var result = new AddBk(
					this,
					context,
					_block,
					doActions,
					undoActions
				).Start();

				if (result != AddBk.Result.Rejected &&
					result != AddBk.Result.ChangeOverRejected)
				{
					context.Commit();
					foreach (Action action in doActions)
						action();
				}
				else
				{
					if (result != AddBk.Result.ChangeOverRejected)
					{
						InitBlockTimestamps();
					}

					foreach (Action action in undoActions)
						action();
				}

				//TODO: only do that if tip has changed
				Tip = GetTip();

				BlockChainTrace.Information("Block " + System.Convert.ToBase64String(Merkle.blockHeaderHasher.Invoke(block.header)) + " is " + result);

				return result;
			}
		}

		public bool HandleNewTransaction(Types.Transaction transaction) //TODO: use Keyed type
		{
			var doActions = new List<Action>();
			var undoActions = new List<Action>();

			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				var result = new AddTx(
					this,
					context,
					new Keyed<Types.Transaction>(Merkle.transactionHasher.Invoke(transaction), transaction),
					doActions,
					undoActions
				).Start();

				if (result != AddTx.Result.Rejected)
				{
					context.Commit(); //TODO: don't need to commit if added to mempool
					foreach (Action action in doActions)
						action();
					return true;
				}
				else
				{
					foreach (Action action in undoActions)
						action();
					return false;
				}
			}
		}

		public bool HandleNewContract(Types.Contract contract)
		{
			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				var contractHash = ContractHelper.Compile(contract.code);

				if (contractHash != null)
				{
					ContractStore.Put(context, new Keyed<Types.Contract>(contractHash, contract));
					context.Commit();

					return true;
				}

				return false;
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
				var bk = MainBlockStore.Get(context, key);

				return bk == null ? null : bk.Value;
			}
		}

		public List<Keyed<Types.Output>> GetUTXOSet()
		{
			using (TransactionContext context = _DBContext.GetTransactionContext())
			{
				return UTXOStore.All(context).ToList();
			}
		}

		//public List<Tuple<Types.Outpoint,Types.Output>> GetUTXOSet()
		//{
		//	var values = new List<Tuple<Types.Outpoint, Types.Output>>();

		//	using (TransactionContext context = _DBContext.GetTransactionContext())
		//	{
		//		foreach (var item in _UTXOStore.All(context))
		//		{
		//			byte[] txHash = new byte[item.Key.Length - 1];
		//			Array.Copy(item.Key, txHash, txHash.Length);

		//			uint index = item.Key[item.Key.Length - 1];

		//			var outpoint = new Types.Outpoint(txHash, index);

		//			values.Add(new Tuple<Types.Outpoint, Types.Output>(outpoint, item.Value));
		//		}
		//	}

		//	return values;
		//}

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
				0,
				new byte[] { },
				new byte[] { },
				new byte[] { },
				ListModule.OfSeq<byte[]>(new List<byte[]>()),
				DateTime.Parse(date).ToBinary(),
				1,
				nonce
			);

			var newBlock = new Types.Block(blockHeader, ListModule.OfSeq<Types.Transaction>(transactions.Select(t => t.Value)));

			if (HandleNewBlock(newBlock) == AddBk.Result.Added)
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

	[TestFixture()]
	public class BlockChainTests
	{
		private readonly Random _Random = new Random();
		private BlockChain _BlockChain;
		private const string DB = "temp";
		private Keyed<Types.Block> _GenesisBlock;

		[SetUp]
		public void RunOnceBeforeEachTest()
		{
			Dispose();
			_GenesisBlock = GetBlock();
			_BlockChain = new BlockChain(DB, _GenesisBlock.Key);
		}

		[TestFixtureTearDown]
		public void Dispose()
		{
			if (_BlockChain != null)
			{
				_BlockChain.Dispose();
			}

			if (Directory.Exists(DB))
			{
				Directory.Delete(DB, true);
			}
		}

		[Test()]
		public void ShouldReorderBlockChain()
		{
			var block1 = GetBlock(_GenesisBlock);
			var block2 = GetBlock(block1);
			var block3 = GetBlock(block2);
			var block4 = GetBlock(block3);
			var sideBlock1 = GetBlock(block1);
			var sideBlock2 = GetBlock(sideBlock1);
			var sideBlock3 = GetBlock(sideBlock2);

			Assert.That(_BlockChain.HandleNewBlock(_GenesisBlock.Value), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.HandleNewBlock(block1.Value), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.HandleNewBlock(block2.Value), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.HandleNewBlock(block3.Value), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.HandleNewBlock(block4.Value), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.HandleNewBlock(sideBlock1.Value), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.HandleNewBlock(sideBlock2.Value), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.HandleNewBlock(sideBlock3.Value), Is.EqualTo(AddBk.Result.Added));
		}

		private Keyed<Types.Block> GetBlock(Keyed<Types.Block> parent)
		{
			return GetBlock(parent.Key);
		}

		private Keyed<Types.Block> GetBlock(byte[] parent = null)
		{
			var nonce = new byte[10];

			_Random.NextBytes(nonce);

			var header = new Types.BlockHeader(
				0,
				parent ?? new byte[] { },
				0,
				new byte[] { },
				new byte[] { },
				new byte[] { },
				ListModule.OfSeq<byte[]>(new List<byte[]>()),
				DateTime.Now.ToFileTimeUtc(),
				0,
				nonce
			);

			var block = new Types.Block(header, ListModule.OfSeq<Types.Transaction>(new List<Types.Transaction>()));
			var key = Merkle.blockHeaderHasher.Invoke(header);

			return new Keyed<Types.Block>(key, block);
		}
	}
}
