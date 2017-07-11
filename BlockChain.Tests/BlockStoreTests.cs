using System;
using NUnit.Framework;
using Store;
using System.Linq;
using System.IO;
using System.Reflection;
using Consensus;

namespace BlockChain.Store
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
		public void ShouldGetOrphan()
		{
			var parent = Utils.GetGenesisBlock();
			var child = parent.Child();

			using (var dbTx = _DBContext.GetTransactionContext())
			{
                PutBlock(dbTx, child, LocationEnum.Main);
				Assert.That(_BlockStore.Orphans(dbTx, BkHash(parent)).Select(t => t.Value), Contains.Item(child));
			}
		}

		[Test()]
		public void ShouldNotGetOrphan()
		{
			var parent = Utils.GetGenesisBlock();
			var child = parent.Child();

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				PutBlock(dbTx, child, LocationEnum.Main);
				Assert.That(_BlockStore.Orphans(dbTx, BkHash(parent)), Is.Empty);
			}
		}

		[Test()]
		public void ShouldHaveChildren()
		{
			var parent = Utils.GetGenesisBlock();
			var child = parent.Child();

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				PutBlock(dbTx, child, LocationEnum.Main);
				Assert.That(_BlockStore.HasChildren(dbTx, BkHash(parent)), Is.True);
			}
		}

		[Test()]
		public void ShouldReassembleBlock()
		{
			var tx = Utils.GetTx();
			var block = Utils.GetGenesisBlock().AddTx(tx);

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				PutBlock(dbTx, block, LocationEnum.Main);

				var retrievedBlock = _BlockChain.BlockStore.GetBlock(dbTx, BkHash(block));

				Assert.That(retrievedBlock.Equals(block), Is.True);
			}
		}

		[Test()]
		public void ShoulGetOrphans()
		{
			var parent = Utils.GetGenesisBlock();
			var child1 = parent.Child();
			var child2 = parent.Child();

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				PutBlock(dbTx, child1, LocationEnum.Main);
				PutBlock(dbTx, child2, LocationEnum.Main);
				Assert.That(_BlockStore.Orphans(dbTx, BkHash(parent)).Count, Is.EqualTo(2));
			}
		}

		[Test()]
		public void ShouldGetLocation()
		{
			var block = Utils.GetGenesisBlock();

			using (var dbTx = _DBContext.GetTransactionContext())
			{
                PutBlock(dbTx, block, LocationEnum.Main);
                Assert.That(_BlockStore.GetLocation(dbTx, BkHash(block)), Is.EqualTo(LocationEnum.Main));
                Assert.That(_BlockStore.IsLocation(dbTx, BkHash(block), LocationEnum.Main), Is.True);
			}
        }

		void PutBlock(TransactionContext dbTx, Types.Block bk, LocationEnum location)
		{
            _BlockStore.Put(dbTx, BkHash(bk), bk, location, 0);
		}

		byte[] BkHash(Types.Block bk)
		{
            return Merkle.blockHeaderHasher.Invoke(bk.header);
		}
	}
}
