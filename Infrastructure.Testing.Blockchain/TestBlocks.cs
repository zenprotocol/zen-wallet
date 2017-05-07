using System;
using System.Collections.Generic;
using Consensus;
using Microsoft.FSharp.Collections;
using Store;
using BlockChain;

namespace Infrastructure.Testing.Blockchain
{
	public class TestBlockBlockChainExpectationPool
	{
		public List<String> List { get; private set; }
		private Dictionary<String, String> Tree { get; set; }
		public Dictionary<String, TestBlock> Blocks { get; set; }
		public Dictionary<String, AddBk.Result> Expectations { get; set; }
		public byte[] GenesisBlockHash { get; set; }

		public TestBlockBlockChainExpectationPool()
		{
			List = new List<String>();
			Tree = new Dictionary<String, String>();
			Blocks = new Dictionary<String, TestBlock>();
			Expectations = new Dictionary<String, AddBk.Result>();
		}

		public void Add(String tag, TestBlock block, AddBk.Result expectedResult, String parent = null) {
			List.Add(tag);

			Blocks[tag] = block;

			if (parent != null)
			{
				Tree[tag] = parent;
				Blocks[parent].Render();
				Blocks[tag].Parent = Blocks[parent];
			}

			block.Render();
			Expectations[tag] = expectedResult;
		}


		public void Render()
		{
			foreach (var testBlock in Blocks.Values)
			{
				testBlock.Render();
			}

			foreach (var item in Tree)
			{
				Blocks[item.Value].Parent = Blocks[item.Key];
			}
		}
	}

	public class TestBlock
	{
//		private Random _Random = new Random();
		public TestBlock Parent { get; set; }
		//public TestTransactionPool _TestTransactionPool { get; set; }
		private Types.Transaction[] _Transactions;
		public Keyed<Types.Block> Value { get; set; }
		public string Tag { get; set; }

		//public TestBlock(TestTransactionPool testTransactionPool)
		//{
		//	_TestTransactionPool = testTransactionPool;
		//}
		public TestBlock(params Types.Transaction[] transactions)
		{
			_Transactions = transactions;
		}

		public void Render()
		{
			if (Value != null)
			{
				return;
			}

			var parentKey = new byte[] { };

			if (Parent != null)
			{
				Parent.Render();
				parentKey = Parent.Value.Key;
			}

			uint version = 1;
			string date = "2000-02-02";

			var blockHeader = new Types.BlockHeader(
				version,
				parentKey,
				0,
				new byte[] { },
				new byte[] { },
				new byte[] { },
				ListModule.OfSeq<byte[]>(new List<byte[]>()),
				DateTime.Parse(date).ToBinary(),
				1,
				new byte[] { }
			);

			//_TestTransactionPool.Render();

			//var block = new Types.Block(blockHeader, ListModule.OfSeq<Types.Transaction>(_TestTransactionPool.Values.Select(t => t.Value.Value)));
			var block = new Types.Block(blockHeader, ListModule.OfSeq<Types.Transaction>(_Transactions));

//			byte[] key = Merkle.blockHasher.Invoke(block);
			byte[] key = Merkle.blockHeaderHasher.Invoke(block.header);

			Value = new Keyed<Types.Block>(key, block);

			TestTrace.Transaction(Tag, key);
		}
	}
}