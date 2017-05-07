using System;
using System.Collections.Generic;
using Consensus;
using Microsoft.FSharp.Collections;
using Store;
using System.Linq;

namespace Infrastructure.Testing.Blockchain
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
			byte[] parentKey = parent == null ? new byte[] {} : Merkle.blockHeaderHasher.Invoke(parent.header);

			Types.BlockHeader newBlockHeader = new Types.BlockHeader(1, parentKey, 0, new byte[] { }, new byte[] { }, new byte[] { }, ListModule.OfSeq(new List<byte[]>()), 0, pdiff, null);
			var transactions = new List<Types.Transaction>();
			FSharpList<Types.Transaction> newBlockTransactions = ListModule.OfSeq(transactions);
			Types.Block newBlock = new Types.Block(newBlockHeader, newBlockTransactions);

			return newBlock;
		}

		public static Types.Output GetOutput()
		{
			return new Types.Output(Consensus.Tests.cbaselock, Consensus.Tests.zspend);
		}

		//public static Keyed<Types.Block> GetGenesisBlock(TestTransactionPool testTransactionPool)
		//{
		//	return GetGenesisBlock(testTransactionPool.ToList().Select(t => t.Value.Value));
		//}

		//public static TestBlock GetGenesisBlock(params Types.Transaction[] transactions)
		//{
		//	UInt32 pdiff = Convert.ToUInt32(0);

		//	var newBlockHeader = new Types.BlockHeader(
		//		1, 
		//		new byte[] { }, 
		//		new byte[] { }, 
		//		new byte[] { }, 
		//		new byte[] { }, 
		//		ListModule.OfSeq(new List<byte[]>()), 
		//		0, 
		//		pdiff, 
		//		null
		//	);

		//	//if (transactions == null)
		//	//{
		//	//	transactions = new List<Types.Transaction>();
		//	//}

		//	var newBlockTransactions = ListModule.OfSeq(transactions);
		//	var newBlock = new Types.Block(newBlockHeader, newBlockTransactions);

		//	var key = Merkle.blockHasher.Invoke(newBlock);
		//	var value = new Keyed<Types.Block>(key, newBlock);

		//	var testBlock = new TestBlock(
		//	return value;
		//}
	}
}