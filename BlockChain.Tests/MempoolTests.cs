using System;
using Infrastructure.Testing;
using NUnit.Framework;
using Wallet.core.Data;

namespace BlockChain
{
	public class MempoolTests : BlockChainTestsBase
	{
		[Test, Order(1)]
		public void ShouldUnorphan()
		{
			var tx = Utils.GetTx();

			var txOrpan = Utils.GetTx().AddInput(tx, 0, new Key().Address);

			_BlockChain.HandleTransaction(txOrpan);
			Assert.That(_BlockChain.memPool.OrphanTxPool.ContainsKey(txOrpan.Key()), Is.True, "should be there");

			_BlockChain.HandleTransaction(tx);
			Assert.That(_BlockChain.memPool.OrphanTxPool.ContainsKey(txOrpan.Key()), Is.True, "should not be there");
		}
	}
}
