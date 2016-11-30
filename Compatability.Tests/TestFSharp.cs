using NUnit.Framework;
using System;
using Consensus;
using System.Collections.Generic;

namespace Compatability.Tests
{
	[TestFixture()]
	public class TestFSharp
	{
		[Test()]
		public void CanUseEquals()
		{
			Types.Outpoint o1 = new Types.Outpoint(new byte[] { 0x00 }, 1);
			Types.Outpoint o2 = new Types.Outpoint(new byte[] { 0x00 }, 1);

			Assert.IsFalse(o1.GetHashCode() != o2.GetHashCode());
			Assert.IsFalse(o1 == o2);
			Assert.IsTrue(o1.Equals(o2));
		}

		[Test()]
		public void CanUseEqualsWithMaps()
		{
			Types.Outpoint o1 = new Types.Outpoint(new byte[] { 0x00 }, 1);
			Types.Outpoint o2 = new Types.Outpoint(new byte[] { 0x00 }, 1);

			IDictionary<Types.Outpoint, Types.Transaction> dictionary = new Dictionary<Types.Outpoint, Types.Transaction>();

			dictionary.Add(o1, Util.GetNewTransaction(1));

			Assert.IsTrue(dictionary.ContainsKey(o1));
			Assert.IsTrue(dictionary.ContainsKey(o2));
		}	

		[Test()]
		public void CanUseEqualsWithLists()
		{
			Types.Outpoint o1 = new Types.Outpoint(new byte[] { 0x00 }, 1);
			Types.Outpoint o2 = new Types.Outpoint(new byte[] { 0x00 }, 1);

			IList<Types.Outpoint> list = new List<Types.Outpoint>();

			list.Add(o1);

			Assert.IsTrue(list.Contains(o1));
			Assert.IsTrue(list.Contains(o2));
		}
	}
}
