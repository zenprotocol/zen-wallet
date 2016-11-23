using NUnit.Framework;
using System;
using Consensus;

namespace BlockChain.Tests
{
	[TestFixture()]
	public class ConsensusTests
	{
		[Test()]
		public void TestCase()
		{
			Types.Block block = Util.GetBlock(null, 0);

			var data = Merkle.serialize<Types.Block>(block);

			Types.Block _block = Serialization.context.GetSerializer<Types.Block>().UnpackSingleObject(data);

			Assert.IsTrue(block.Equals(_block));
		}

		[Test()]
		public void CanSerDesTx()
		{
			var p = new TestTransactionPool();

			p.Add("test", 1);
			p.Render();

			var test = p.TakeOut("test");

			var data = Merkle.serialize<Types.Transaction>(test.Value);

			var t = Serialization.context.GetSerializer<Types.Transaction>().UnpackSingleObject(data);

			Assert.IsTrue(t.Equals(test));
		}

		[Test()]
		public void CanSerDesTx2()
		{
			var tx = Consensus.Tests.tx;
			var data = Merkle.serialize<Types.Transaction>(tx);

			var t = Serialization.context.GetSerializer<Types.Transaction>().UnpackSingleObject(data);

			Assert.IsTrue(t.Equals(tx));
		}

		[Test()]
		public void CanHasHtransaction()
		{
			var hash = Merkle.transactionHasher.Invoke(Consensus.Tests.tx);

			Assert.IsNotNull(hash);

		}
	}
}
