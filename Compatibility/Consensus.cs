using System;
using System.Collections.Generic;
using System.Text;
using Consensus;
using Microsoft.FSharp.Collections;

namespace Compatibility
{
	class Consensus
	{
		public static void start()
		{
			byte[] hashed = Merkle.transactionHasher.Invoke(GetNewTransaction());
			Console.WriteLine(Encoding.ASCII.GetString(hashed));


			//Types.Block block = GetBlock(null, 0);
			//var data = Merkle.serialize<Types.Block>(block);
			//Types.Block _block = Serialization.context.GetSerializer<Types.Block>().UnpackSingleObject(data);

			//Assert.IsTrue(block.Equals(_block));





			Console.ReadLine ();
		}

		private static Types.Transaction GetNewTransaction()
		{
			var endpoints = new List<Types.Outpoint>();
			var outputs = new List<Types.Output>();
			var hashes = new List<byte[]>();

			endpoints.Add(new Types.Outpoint(new byte[] { 0x34 }, 222));

			Types.Transaction transaction =
				new Types.Transaction(1,
					ListModule.OfSeq(endpoints),
					ListModule.OfSeq(hashes),
					ListModule.OfSeq(outputs),
					null);

			return transaction;
		}

		private static Types.Block GetBlock(Types.Block parent, Double difficulty)
		{
			UInt32 pdiff = Convert.ToUInt32(difficulty);
			//byte[] parentKey = parent == null ? null : Merkle.blockHasher.Invoke(parent);
			byte[] parentKey = parent == null ? null : Merkle.blockHeaderHasher.Invoke(parent.header);

			Types.BlockHeader newBlockHeader = new Types.BlockHeader(1, parentKey, null, null, null, null, 0, pdiff, null);
			var transactions = new List<Types.Transaction>();
			FSharpList<Types.Transaction> newBlockTransactions = ListModule.OfSeq(transactions);
			Types.Block newBlock = new Types.Block(newBlockHeader, newBlockTransactions);

			return newBlock;
		}
	}
}
