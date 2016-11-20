using NUnit.Framework;
using System;
using Consensus;
using System.Collections.Generic;
using System.Linq;
using BlockChain.Data;

namespace BlockChain.Tests
{
	[TestFixture()]
	public class TestCSharp
	{
		[Test()]
		public void EqualityComparerTest()
		{
			byte[] key1 = Merkle.transactionHasher.Invoke(Util.GetNewTransaction(1));
			byte[] key2 = Merkle.transactionHasher.Invoke(Util.GetNewTransaction(1));

			EqualityComparer<byte[]> equalityComparer = EqualityComparer<byte[]>.Default;

			Assert.IsFalse(key1 == key2); // should fail
			Assert.IsFalse(key1.Equals(key2)); // should fail
			Assert.IsFalse(equalityComparer.Equals(key1, key2)); // should fail

			Assert.IsTrue(Enumerable.SequenceEqual(key1, key2));

			ByteArrayComparer byteArrayComparer = new ByteArrayComparer();
			Assert.IsTrue(byteArrayComparer.Equals(key1, key2));
		}

		[Test()]
		public void HashDictionaryTest()
		{
			HashDictionary<Types.Transaction> dict = new HashDictionary<Types.Transaction>();

			byte[] key1 = Merkle.transactionHasher.Invoke(Util.GetNewTransaction(1));
			byte[] key2 = Merkle.transactionHasher.Invoke(Util.GetNewTransaction(1));

			dict.Add(key1, Util.GetNewTransaction(1));

			Assert.IsTrue(dict.ContainsKey(key1));
			Assert.IsTrue(dict.ContainsKey(key2));
		}
	}
}
