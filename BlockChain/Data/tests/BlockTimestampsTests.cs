using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Store;
using System.Linq;
using System.Collections;

namespace BlockChain.Data.Tests
{
	[TestFixture()]
	public class BlockTimestampsTests
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
}
