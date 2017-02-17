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
			var tx = Utils.GetTx().Sign();

			var txInvalidOrpan = Utils.GetTx().AddInput(tx, 0);

			_BlockChain.HandleTransaction(txInvalidOrpan);
			Assert.That(_BlockChain.memPool.OrphanTxPool.ContainsKey(txInvalidOrpan.Key()), Is.True, "should be there");

			_BlockChain.HandleTransaction(tx);

			Reset();

			Assert.That(_BlockChain.memPool.OrphanTxPool.ContainsKey(txInvalidOrpan.Key()), Is.False, "should not be there");
		}
	}
}
