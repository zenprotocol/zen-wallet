using System;
using System.Collections.Generic;
using Consensus;
using Microsoft.FSharp.Collections;

namespace BlockChain.Tests
{
	public class Util
	{
		public static Types.Block GetBlock(Types.Block parent, Double difficulty)
		{
			UInt32 pdiff = Convert.ToUInt32(difficulty);
			byte[] parentKey = parent == null ? new byte[] {} : Merkle.blockHasher.Invoke(parent);

			Types.BlockHeader newBlockHeader = new Types.BlockHeader(1, parentKey, new byte[] { }, new byte[] { }, new byte[] { }, ListModule.OfSeq(new List<byte[]>()), 0, pdiff, null);
			var transactions = new List<Types.Transaction>();
			FSharpList<Types.Transaction> newBlockTransactions = ListModule.OfSeq(transactions);
			Types.Block newBlock = new Types.Block(newBlockHeader, newBlockTransactions);

			return newBlock;
		}
	}
}
