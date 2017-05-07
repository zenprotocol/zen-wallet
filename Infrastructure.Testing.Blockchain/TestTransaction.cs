using System;
using System.Collections.Generic;
using BlockChain;
using Consensus;
using Microsoft.FSharp.Collections;
using Store;

namespace Infrastructure.Testing.Blockchain
{
	public class TestTransactionBlockChainExpectationPool : TestPool<TestTransactionBlockChainExpectation>
	{
		public void Add(string tag, int outputs, AddTx.Result result)
		{
			Add(tag, new TestTransactionBlockChainExpectation()
			{
				Outputs = outputs,
				Result = result,
				Tag = tag
			});
		}
	}

	public class TestTransactionPool : TestPool<TestTransaction>
	{
	}

	public class TestPool<T> where T : TestTransaction, new()
	{
		private Dictionary<String, T> _Map { get; set; } //TODO: use OrderedDictionary

		public TestPool()
		{
			_Map = new Dictionary<String, T>();
		}

		public Keyed<Types.Transaction> this[String tag]
		{
			get
			{
				return GetItem(tag).Value;
			}
		}

		public T GetItem(String tag/*, bool remove = false*/)
		{
			return _Map[tag];
		}

		public void Add(string tag, int outputs)
		{
			Add(tag, new T()
			{
				Outputs = outputs,
				Tag = tag
			});
		}

		protected void Add(string tag, T t)
		{
			_Map[tag] = t;
		}

		public void Spend(string tag, string previousTag, uint outputIndex)
		{
			_Map[tag].Inputs.Add(
				new Point() { 
					RefTransaction = _Map[previousTag], 
					Index = outputIndex 
				}
			);
		}

		public Keyed<Types.Transaction> TakeOut(string tag)
		{
			T t = _Map[tag];

			_Map.Remove(tag);

			return t.Value;
		}

		public void Render()
		{
			foreach (TestTransaction testTransaction in _Map.Values)
			{
				testTransaction.Render();
			}
		}

		public Dictionary<String, T>.KeyCollection Keys
		{
			get
			{
				return _Map.Keys;
			}
		}

		public Dictionary<String, T>.ValueCollection Values
		{
			get
			{
				return _Map.Values;
			}
		}

		public List<T> ToList()
		{
			var list = new List<T>();

			foreach (var item in _Map)
			{
				list.Add(item.Value);
			}

			return list;
		}
	}

	public class Point
	{
		public TestTransaction RefTransaction { get; set; }
		public uint Index { get; set; }
	}

	public class TestTransactionBlockChainExpectation : TestTransaction
	{
		public AddTx.Result Result { get; set; }
	}

	public class TestTransaction
	{
		//private Random _Random = new Random();
		public int Outputs { get; set; }
		public List<Point> Inputs { get; set; }
		public Keyed<Types.Transaction> Value { get; set; }
		public string Tag { get; set; }

		public TestTransaction()
		{
			Inputs = new List<Point>();
			Value = null;
		}

		public void Render()
		{
			if (Value != null)
			{
				return;
			}

			var outputs = new List<Types.Output>();

			for (var i = 0; i < Outputs; i++)
			{
				outputs.Add(Util.GetOutput());
			}

			var inputs = new List<Types.Outpoint>();

			foreach (Point point in Inputs)
			{
				point.RefTransaction.Render();
				inputs.Add(new Types.Outpoint(point.RefTransaction.Value.Key, point.Index));
			}

			var hashes = new List<byte[]>();

			//hack Concensus into giving a different hash per each tx created
			var version = (uint) 1;//_Random.Next(1000);

			Types.Transaction transaction = new Types.Transaction(version,
				ListModule.OfSeq(inputs),
				ListModule.OfSeq(hashes),
				ListModule.OfSeq(outputs),
				null);

			byte[] key = Merkle.transactionHasher.Invoke(transaction);
			Value = new Keyed<Types.Transaction>(key, transaction);

			TestTrace.Transaction(Tag, key);
		}
	}
}