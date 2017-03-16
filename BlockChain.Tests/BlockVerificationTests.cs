using System;
using System.Collections.Generic;
using Consensus;
using Microsoft.FSharp.Collections;
using NUnit.Framework;

namespace BlockChain
{
	[TestFixture()]
	public class BlockVerificationTests : BlockChainTestsBase
	{
		[Test]
		public void ShouldRejectFutureTimestampedBlock()
		{
			_BlockChain.HandleBlock(_GenesisBlock);
			var block = Child(_GenesisBlock, 1);
			Assert.That(_BlockChain.HandleBlock(block), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));
			block = Child(block, 2);
			Assert.That(_BlockChain.HandleBlock(block), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));
			block = Child(block, 3);
			Assert.That(_BlockChain.HandleBlock(block), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Rejected));
		}

		Types.Block Child(Types.Block bk, int addHours)
		{
			var nonce = new byte[10];
			new Random().NextBytes(nonce);
			//var nonce = new byte[] { };

			var timestamp = DateTime.Now.AddHours(addHours).Ticks;

			var header = new Types.BlockHeader(
				0,
				Merkle.blockHeaderHasher.Invoke(bk.header),
				bk.header.blockNumber + 1,
				new byte[] { },
				new byte[] { },
				new byte[] { },
				ListModule.OfSeq<byte[]>(new List<byte[]>()),
				timestamp,
				0,
				nonce
			);

			var txs = new List<Types.Transaction>();
			txs.Add(Utils.GetTx());

			return new Types.Block(header, ListModule.OfSeq(txs));
		}
	}
}
