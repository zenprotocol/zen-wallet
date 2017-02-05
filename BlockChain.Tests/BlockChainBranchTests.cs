//using System;
//using Consensus;
//using BlockChain.Store;
//using Store;
//using Infrastructure;
//using System.Text;
//using System.Collections.Generic;
//using Microsoft.FSharp.Collections;
//using System.Linq;
//using BlockChain.Data;
//using NUnit.Framework;
//using System.IO;

//namespace BlockChain.Tests
//{
//	[TestFixture()]
//	public class BlockChainBranchTests : BlockChainTestsBase
//	{
//		[Test()]
//		public void ShouldDetectBranch()
//		{
//			var block1 = GetBlock(_GenesisBlock);
//			var block2 = GetBlock(_GenesisBlock);

//			Assert.That(_BlockChain.HandleNewBlock(_GenesisBlock), Is.EqualTo(AddBk.Result.Added));
//			Assert.That(_BlockChain.HandleNewBlock(block1.Value), Is.EqualTo(AddBk.Result.Added));
//			Assert.That(_BlockChain.HandleNewBlock(block2.Value), Is.EqualTo(AddBk.Result.Added));

//			Assert.That(Location(_GenesisBlock), Is.EqualTo(LocationEnum.Main));
//			Assert.That(Location(block1), Is.EqualTo(LocationEnum.Main));

//			// detect branching
//			Assert.That(Location(block2), Is.EqualTo(LocationEnum.Branch));

//			// detect branch child
//			var block3 = GetBlock(block2);
//			Assert.That(_BlockChain.HandleNewBlock(block3.Value), Is.EqualTo(AddBk.Result.Added));
//			Assert.That(Location(block3), Is.EqualTo(LocationEnum.Main));
//		}
//	}
	
//}
