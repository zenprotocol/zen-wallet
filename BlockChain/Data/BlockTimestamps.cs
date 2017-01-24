using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Store;
using System.Linq;
using System.Collections;

namespace BlockChain.Data
{
	public class BlockTimestamps
	{
		public const int SIZE = 11;
		private readonly List<long> _Values = new List<long>();

		public void Init(params long[] values)
		{
			_Values.Clear();

			for (var i = 0; i < SIZE - values.Count(); i++)
			{
				_Values.Add(values[0]);
			}

			for (var i = 0; i < values.Count(); i++)
			{
				_Values.Add(values[i]);
			}
		}

		public void Push(long value)
		{
			_Values.Add(value);

			if (_Values.Count > SIZE)
			{
				_Values.RemoveAt(0);
			}
		}

		public long Median()
		{
			_Values.Sort();
			return _Values.ElementAt(5);
		}
	}

	[TestFixture()]
	public class TimestampsTests
	{
		[Test()]
		public void ShouldInitWithPartialList()
		{
			var timestamps = new BlockTimestamps();

			timestamps.Init(1);
			Assert.That(timestamps.Median(), Is.EqualTo(1));
		}

		[Test()]
		public void ShouldInitWithPartialList1()
		{
			var timestamps = new BlockTimestamps();

			timestamps.Init(1, 100);
			Assert.That(timestamps.Median(), Is.EqualTo(1));
		}

		[Test()]
		public void ShouldInitWithList()
		{
			var timestamps = new BlockTimestamps();
			var list = new List<long>();

			for (long i = 0; i < 11; i++)
			{
				list.Add(i);
			}

			timestamps.Init(list.ToArray());

			Assert.That(timestamps.Median(), Is.EqualTo(list.ElementAt(5)));
		}
	}

	/*
	[TestFixture()]
	public class TimestampsStoreTests
	{
		//private DBreeze.DBreezeEngine _DBreezeEngine;
		private BlockTimestamps _TimestampsStore;
		private const int MAX_VALUES = 11; // TODO
		//private const string TABLE = "timestamps";

		//private const string DB = "temp";
			
		[TestFixtureSetUp]
		public void Init()
		{
			//Dispose();
			//_DBreezeEngine = new DBreeze.DBreezeEngine(DB);
			_TimestampsStore = new BlockTimestamps();
		}

		//[TestFixtureTearDown]
		//public void Dispose()
		//{
		//	if (_DBreezeEngine != null)
		//	{
		//		_DBreezeEngine.Dispose();
		//	}

		//	if (Directory.Exists(DB))
		//	{
		//		Directory.Delete(DB, true);
		//	}
		//}

		//[Test()]
		//public void ShouldNotExceedMaxItemsCount()
		//{
		//	using (var dbTx = _DBreezeEngine.GetTransaction())
		//	{
		//		for (long i = 0; i < 20; i++)
		//		{
		//			_TimestampsStore.Add(dbTx, i);
		//			Assert.That(dbTx.Count(TABLE), Is.LessThanOrEqualTo(MAX_VALUES));
		//			//Assert.That(dbTx.Select<long, long>(TABLE, i).Exists, Is.True);
		//		}
		//	}
		//}

		[Test()]
		public void ShouldGetMedian()
		{
			var random = new Random();

			//using (var dbTx = _DBreezeEngine.GetTransaction())
			//{
				var list = new List<long>();

				for (int i = 0; i < 20; i++)
				{
					var value = (long)random.Next(100);

					list.Add(value);

					_TimestampsStore.Push(value);

					var idx = list.Count;
					var half = Math.Min(list.Count, MAX_VALUES) / 2;
					idx -= half;
					idx -= 1;
					var expectedMedian = list.ElementAt(idx);

					if (Math.Min(list.Count, MAX_VALUES) % 2 == 0 && list.Count > idx)
					{
						expectedMedian += list.ElementAt(idx + 1);
						expectedMedian /= 2;
					}

					Assert.That(_TimestampsStore.GetMedian(), Is.EqualTo(expectedMedian), "index: " + i);
				}
			//}
		}
	}
	*/
}
