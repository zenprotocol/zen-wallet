using System;
using System.Collections.Generic;
using System.Linq;
using Consensus;
using Microsoft.FSharp.Collections;
using Store;

namespace Infrastructure.Testing
{
	public class Utils
	{
		public static Types.Block GetGenesisBlock()
		{
			var nonce = new byte[10];

			new Random().NextBytes(nonce);

			var header = new Types.BlockHeader(
				0,
				new byte[] { },
				0,
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

		public static Types.Transaction GetTx()
		{
			return new Types.Transaction(
				0,
				ListModule.OfSeq(new List<Types.Outpoint>()),
				ListModule.OfSeq(new List<byte[]>()),
				ListModule.OfSeq(new List<Types.Output>()),
				null);
		}
	}
}
