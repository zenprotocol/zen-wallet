using System;
using System.Collections.Generic;
using System.Linq;
using Consensus;
using Microsoft.FSharp.Collections;
using Store;

namespace Infrastructure.Testing
{
	public static class BlockExtensions
	{
		public static string GetTag(this Types.Block block)
		{
			return FieldEx<Types.Block, string>.Get(block);
		}

		public static void SetTag(this Types.Block block, string value)
		{
			FieldEx<Types.Block, string>.Set(block, value);
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
				DateTime.Now.ToFileTimeUtc(),
				0,
				nonce
			);

			return new Types.Block(header, ListModule.OfSeq<Types.Transaction>(new List<Types.Transaction>()));
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
				DateTime.Now.ToFileTimeUtc(),
				0,
				nonce
			);

			var txs = bk.transactions.ToList();

			txs.Add(tx);

			return new Types.Block(bk.header, ListModule.OfSeq<Types.Transaction>(txs));
		}
	}
}
