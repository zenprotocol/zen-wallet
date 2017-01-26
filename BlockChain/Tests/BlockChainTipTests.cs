using System;
using Consensus;
using BlockChain.Store;
using Store;
using Infrastructure;
using System.Text;
using System.Collections.Generic;
using Microsoft.FSharp.Collections;
using System.Linq;
using BlockChain.Data;
using NUnit.Framework;
using System.IO;

namespace BlockChain.Tests
{
	[TestFixture()]
	public class BlockChainTipTests : BlockChainTestsBase
	{
		Keyed<Types.Block> block1;
		Keyed<Types.Block> block2;
		Keyed<Types.Block> block3;

		[Test, Order(1)]
		public void TipShouldBeNull()
		{
			Assert.That(_BlockChain.Tip, Is.Null);
		}

		[Test, Order(2)]
		public void TipShouldBeOfGenesis()
		{
			Assert.That(_BlockChain.HandleNewBlock(_GenesisBlock.Value), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.Tip, Is.Not.Null);
			Assert.That(_BlockChain.Tip.Key, Is.EqualTo(_GenesisBlock.Key));
		}

		[Test, Order(3)]
		public void TipShouldBeOfNewBlock()
		{
			block1 = GetBlock(_GenesisBlock);
			Assert.That(_BlockChain.HandleNewBlock(block1.Value), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.Tip.Key, Is.EqualTo(block1.Key));
		}


		[Test, Order(4)]
		public void TipShouldNotBecomeNewBlock()
		{
			block2 = GetBlock(_GenesisBlock);
			Assert.That(_BlockChain.HandleNewBlock(block2.Value), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.Tip.Key, Is.EqualTo(block1.Key));
		}

		[Test, Order(5)]
		public void TipShouldBecomeBranch()
		{
			block3 = GetBlock(block2);
			Assert.That(_BlockChain.HandleNewBlock(block3.Value), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.Tip.Key, Is.EqualTo(block3.Key));
		}
	}
}