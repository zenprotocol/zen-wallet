using System;
using System.Collections.Generic;
using BlockChain.Store;
using Consensus;
using DBreeze;
using Microsoft.FSharp.Collections;
using NUnit.Framework;
using Store;
using System.Linq;
using System.IO;
using System.Reflection;
using BlockChain.Tests;
using System.Linq;

namespace BlockChain.Store.Tests
{
	[TestFixture()]
	public class BlockStoreTests : BlockChainTestsBase
	{
		private BlockStore _BlockStore;
		private DBContext _DBContext;
		private const string DB = "temp";

		[OneTimeSetUp]
		public void TestsInit()
		{
			Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			Dispose();
			_DBContext = new DBContext(DB);
			_BlockStore = new BlockStore();
		}

		[OneTimeTearDown]
		public void Dispose()
		{
			if (_DBContext != null)
			{
				_DBContext.Dispose();
			}

			if (Directory.Exists(DB))
			{
				Directory.Delete(DB, true);
			}
		}

		[Test()]
		public void ShouldGetChild()
		{
			var parent = GetBlock();
			var child = GetBlock(parent);

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				_BlockStore.Put(dbTx, child, LocationEnum.Main, 0);
				Assert.That(_BlockStore.Children(dbTx, parent.Key, false).Select(t => t.Value), Contains.Item(child.Value));
			}
		}

		[Test()]
		public void ShouldNotGetChild()
		{
			var parent = GetBlock();
			var child = GetBlock(parent);

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				_BlockStore.Put(dbTx, child, LocationEnum.Main, 0);
				Assert.That(_BlockStore.Children(dbTx, parent.Key, true), Is.Empty);
			}
		}

		[Test()]
		public void ShouldHaveChildren()
		{
			var parent = GetBlock();
			var child = GetBlock(parent);

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				_BlockStore.Put(dbTx, child, LocationEnum.Main, 0);
				Assert.That(_BlockStore.HasChildren(dbTx, parent.Key), Is.True);
			}
		}

		[Test()]
		public void ShouldStoreTx()
		{
			var tx = GetTx();
			var block = GetBlock(tx);

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				_BlockStore.Put(dbTx, block, LocationEnum.Main, 0);
				Assert.That(_BlockStore.Transactions(dbTx, block.Key).Select(t=>t.Key), Contains.Item(tx.Key));
			}
		}

		[Test()]
		public void ShouldReassembleBlock()
		{
			var tx = GetTx();
			var block = GetBlock(tx);

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				_BlockStore.Put(dbTx, block, LocationEnum.Main, 0);

				var retrievedBlock = _BlockChain.BlockStore.GetBlock(dbTx, block.Key);

				Assert.That(retrievedBlock.Equals(block), Is.True);
			}
		}

		[Test()]
		public void ShoulGetChildren()
		{
			var parent = GetBlock();
			var child1 = GetBlock(parent);
			var child2 = GetBlock(parent);

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				_BlockStore.Put(dbTx, child1, LocationEnum.Main, 0);
				_BlockStore.Put(dbTx, child2, LocationEnum.Main, 0);
				Assert.That(_BlockStore.Children(dbTx, parent.Key, false).Count, Is.EqualTo(2));
			}
		}

		[Test()]
		public void ShouldGetLocation()
		{
			var block = GetBlock();

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				_BlockStore.Put(dbTx, block, LocationEnum.Main, 0);
				Assert.That(_BlockStore.GetLocation(dbTx, block.Key), Is.EqualTo(LocationEnum.Main));
				Assert.That(_BlockStore.IsLocation(dbTx, block.Key, LocationEnum.Main), Is.True);
			}
		}
	}
}
