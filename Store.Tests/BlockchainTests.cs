using NUnit.Framework;
using System;
using Consensus;
using System.Collections.Generic;
using System.Linq;
using Microsoft.FSharp.Collections;
using System.IO;

namespace Store.Tests
{
	[TestFixture()]
	public class BlockchainTests
	{
		[Test()]
		public void CanUpdateNewBlockTip()
		{
			Types.Block newBlock = GetBlock();

			String dbName = "test-" + new Random().Next(0, 1000);

			var random = new Random();
			int expectedTip = 0;

			for (int i = 0; i < 10; i++)
			{
				int newTip = random.Next(0, 1000);

				using (BlockChain blockChain = new BlockChain(dbName))
				{
					blockChain.HandleNewValueBlock(newBlock, newTip);
				}

				if (newTip > expectedTip)
				{
					expectedTip = newTip;
				}

				using (DBContext dbContext = new DBContext(dbName))
				{
					var Tip = new Field<int>(dbContext.GetTransactionContext(), "blockchain", "tip");

					Assert.AreEqual(Tip.Value, expectedTip);
				}
			}

			Directory.Delete(dbName, true);
		}

		[Test()]
		public void CanHandleNewValidBlock()
		{
			Types.Block newBlock = GetBlock();

			String dbName = "test-" + new Random().Next(0, 1000);

			using (BlockChain blockChain = new BlockChain(dbName))
			{
				blockChain.HandleNewValueBlock(newBlock, 1);
			}

			Directory.Delete(dbName, true);
		}


		private Types.Block GetBlock()
		{
			Types.BlockHeader newBlockHeader = new Types.BlockHeader(1, null, null, null, null, null, 0, 0, null);
			var transactions = new List<Types.Transaction>();
			FSharpList<Types.Transaction> newBlockTransactions = ListModule.OfSeq(transactions);
			Types.Block newBlock = new Types.Block(newBlockHeader, newBlockTransactions);

			return newBlock;
		}
	}
}
