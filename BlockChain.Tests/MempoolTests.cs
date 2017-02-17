using System;
using Infrastructure.Testing;
using NUnit.Framework;
using Wallet.core.Data;

namespace BlockChain
{
	public class MempoolTests : BlockChainTestsBase
	{
		[Test, Order(1)]
		public void ShouldRemoveUnorphanInvalidTx()
		{
			var tx = Utils.GetTx();

			var txOrpan = Utils.GetTx().AddInput(tx, 0, new Key().Address);

			_BlockChain.HandleTransaction(txOrpan);
			Assert.That(_BlockChain.memPool.OrphanTxPool.ContainsKey(txOrpan.Key()), Is.True, "should be there");

			_BlockChain.HandleTransaction(tx);

			System.Threading.Thread.Sleep(500);

			Assert.That(_BlockChain.memPool.OrphanTxPool.ContainsKey(txOrpan.Key()), Is.False, "should not be there");
		}
	}
}
