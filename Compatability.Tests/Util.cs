using System;
using System.Collections.Generic;
using Consensus;
using Microsoft.FSharp.Collections;

namespace Compatability.Tests
{
	public class Util
	{
		public static Types.Transaction GetNewTransaction(uint version)
		{
			return Consensus.Tests.txwithoutcontract;
			//var outpoints = new List<Types.Outpoint>();
			//var outputs = new List<Types.Output>();
			//var hashes = new List<byte[]>();

			//outpoints.Add(new Types.Outpoint(new byte[] { 0x34 }, 111));

			//Types.Transaction transaction =
			//	new Types.Transaction(version,
			//		ListModule.OfSeq(outpoints),
			//		ListModule.OfSeq(hashes),
			//		ListModule.OfSeq(outputs),
			//		null);

			//return transaction;
		}

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

		public static Types.Output GetOutput()
		{
			return new Types.Output(Consensus.Tests.cbaselock, new Types.Spend(new byte[] { }, 0));
		}
	}
}
