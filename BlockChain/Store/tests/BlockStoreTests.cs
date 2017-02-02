using System;
using NUnit.Framework;
using Store;
using System.Linq;
using System.IO;
using System.Reflection;
using BlockChain.Tests;
using System.Linq;
using Infrastructure.Testing;
using Consensus;

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
			var parent = Utils.GetGenesisBlock();
			var child = parent.Value.Child();

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				_BlockStore.Put(dbTx, Keyed(child), LocationEnum.Main, 0);
				Assert.That(_BlockStore.Children(dbTx, parent.Key, false).Select(t => t.Value), Contains.Item(child));
			}
		}

		[Test()]
		public void ShouldNotGetChild()
		{
			var parent = Utils.GetGenesisBlock();
			var child = parent.Value.Child();

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				_BlockStore.Put(dbTx, Keyed(child), LocationEnum.Main, 0);
				Assert.That(_BlockStore.Children(dbTx, parent.Key, true), Is.Empty);
			}
		}

		[Test()]
		public void ShouldHaveChildren()
		{
			var parent = Utils.GetGenesisBlock();
			var child = parent.Value.Child();

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				_BlockStore.Put(dbTx, Keyed(child), LocationEnum.Main, 0);
				Assert.That(_BlockStore.HasChildren(dbTx, parent.Key), Is.True);
			}
		}

		[Test()]
		public void ShouldStoreTx()
		{
			var tx = Utils.GetTx();
			var block = Utils.GetGenesisBlock().Value.AddTx(tx);

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				_BlockStore.Put(dbTx, Keyed(block), LocationEnum.Main, 0);
				Assert.That(_BlockStore.Transactions(dbTx, Keyed(block).Key).Select(t=>t.Key), Contains.Item(tx));
			}
		}

		[Test()]
		public void ShouldReassembleBlock()
		{
			var tx = Utils.GetTx();
			var block = Utils.GetGenesisBlock().Value.AddTx(tx);

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				_BlockStore.Put(dbTx, Keyed(block), LocationEnum.Main, 0);

				var retrievedBlock = _BlockChain.BlockStore.GetBlock(dbTx, Keyed(block).Key);

				Assert.That(retrievedBlock.Equals(block), Is.True);
			}
		}

		[Test()]
		public void ShoulGetChildren()
		{
			var parent = Utils.GetGenesisBlock();
			var child1 = parent.Value.Child();
			var child2 = parent.Value.Child();

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				_BlockStore.Put(dbTx, Keyed(child1), LocationEnum.Main, 0);
				_BlockStore.Put(dbTx, Keyed(child2), LocationEnum.Main, 0);
				Assert.That(_BlockStore.Children(dbTx, parent.Key, false).Count, Is.EqualTo(2));
			}
		}

		[Test()]
		public void ShouldGetLocation()
		{
			var block = Utils.GetGenesisBlock();

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				_BlockStore.Put(dbTx, block, LocationEnum.Main, 0);
				Assert.That(_BlockStore.GetLocation(dbTx, block.Key), Is.EqualTo(LocationEnum.Main));
				Assert.That(_BlockStore.IsLocation(dbTx, block.Key, LocationEnum.Main), Is.True);
			}
		}

		private Keyed<Types.Block> Keyed(Types.Block bk)
		{
			return new Keyed<Types.Block>(Merkle.blockHeaderHasher.Invoke(bk.header), bk);
		}
	}
}
