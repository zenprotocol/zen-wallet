using System;
using System.Collections.Generic;
using System.Linq;
using Consensus;
using Microsoft.FSharp.Collections;

namespace BlockChain
{
	public static class BlockExtensions
	{
		public static Types.Block Tag(this Types.Block block, string value)
		{
			BlockChainTrace.SetTag(block, value);
			return block;
		}

		public static Types.Block Child(this Types.Block bk)
		{
			var nonce = new byte[10];
			new Random().NextBytes(nonce);
			//var nonce = new byte[] { };

			var header = new Types.BlockHeader(
				0,
				Merkle.blockHeaderHasher.Invoke(bk.header),
				bk.header.blockNumber + 1,
				new byte[] { },
				new byte[] { },
				new byte[] { },
				ListModule.OfSeq<byte[]>(new List<byte[]>()),
				DateTime.Now.Ticks,
				0,
				nonce
			);

			var txs = new List<Types.Transaction>();
			txs.Add(Utils.GetTx());

			return new Types.Block(header, ListModule.OfSeq(txs));
		}

		public static Types.Block AddTx(this Types.Block bk, Types.Transaction tx)
		{
			//var nonce = new byte[10];
			//new Random().NextBytes(nonce);
			var nonce = new byte[] { };

			var header = new Types.BlockHeader(
				0,
				new byte[] { },
				bk.header.blockNumber + 1,
				new byte[] { },
				new byte[] { },
				new byte[] { },
				ListModule.OfSeq<byte[]>(new List<byte[]>()),
				DateTime.Now.Ticks,
				0,
				nonce
			);

			var txs = bk.transactions.ToList();

			txs.Add(tx);

			return new Types.Block(bk.header, ListModule.OfSeq<Types.Transaction>(txs));
		}
	}
}
