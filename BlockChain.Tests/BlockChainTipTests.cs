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
using Infrastructure.Testing;

namespace BlockChain.Tests
{
	[TestFixture()]
	public class BlockChainTipTests : BlockChainTestsBase
	{
		Types.Block block1;
		Types.Block block2;
		Types.Block block3;

		[Test, Order(1)]
		public void TipShouldBeNull()
		{
			Assert.That(_BlockChain.Tip, Is.Null);
		}

		[Test, Order(2)]
		public void TipShouldBeOfGenesis()
		{
			Assert.That(_BlockChain.HandleNewBlock(_GenesisBlock), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.Tip, Is.Not.Null);
			Assert.That(_BlockChain.Tip.Value, Is.EqualTo(_GenesisBlock.header));
		}

		[Test, Order(3)]
		public void TipShouldBeOfNewBlock()
		{
			block1 = _GenesisBlock.Child();
			Assert.That(_BlockChain.HandleNewBlock(block1), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.Tip.Value.Equals(block1.header), Is.True);
		}


		[Test, Order(4)]
		public void TipShouldNotBecomeNewBlock()
		{
			block2 = _GenesisBlock.Child();
			Assert.That(_BlockChain.HandleNewBlock(block2), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.Tip.Value.Equals(block1.header), Is.True);
		}

		[Test, Order(5)]
		public void TipShouldBecomeBranch()
		{
			block3 = block2.Child();
			Assert.That(_BlockChain.HandleNewBlock(block3), Is.EqualTo(AddBk.Result.Added));
			Assert.That(_BlockChain.Tip.Value.Equals(block3.header), Is.True);
		}
	}
}