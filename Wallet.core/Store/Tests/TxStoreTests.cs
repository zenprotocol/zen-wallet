using NUnit.Framework;
using System;
using Wallet.core.Store;
using Store;
using System.IO;
using System.Reflection;
using BlockChain.Data;
using System.Collections.Generic;
using Consensus;
using Microsoft.FSharp.Collections;

namespace Wallet.core
{
	[TestFixture()]
	public class TxStoreTests
	{
		private TxBalancesStore _TxBalancesStore;
		private DBContext _DBContext;
		private const string DB = "temp";

		[OneTimeSetUp]
		public void TestsInit()
		{
			Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			Dispose();
			_DBContext = new DBContext(DB);
			_TxBalancesStore = new TxBalancesStore();
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
		public void ShouldContainKey()
		{
			var tx = GetTx();

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				_TxBalancesStore.Put(dbTx, tx, new HashDictionary<long>(), TxStateEnum.Confirmed);
				dbTx.Commit();
			}

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				Assert.That(_TxBalancesStore.ContainsKey(dbTx, tx.Key), Is.EqualTo(true));
			}
		}

		[Test()]
		public void ShouldIncrementIdentity()
		{
			var tx = GetTx();

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				_TxBalancesStore.Put(dbTx, tx, new HashDictionary<long>(), TxStateEnum.Confirmed);
				dbTx.Commit();
			}
		}


		[Test()]
		public void ShouldNotContainKey()
		{
			var tx = GetTx();

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				_TxBalancesStore.Put(dbTx, tx, new HashDictionary<long>(), TxStateEnum.Confirmed);

				Assert.That(_TxBalancesStore.ContainsKey(dbTx, new byte[] { 0xFF, 0xFF }), Is.EqualTo(false));
			}
		}


		[Test()]
		public void ShouldContainObject()
		{
			var tx = GetTx();

			var x = new HashDictionary<long>();

			x[new byte[] { 0x010, 0x020 }] = 10;
			x[new byte[] { 0x020, 0x030 }] = 100;

			using (var dbTx = _DBContext.GetTransactionContext())
			{
				_TxBalancesStore.Put(dbTx, tx, x, TxStateEnum.Confirmed);

				var balances = _TxBalancesStore.Balances(dbTx, tx.Key);
				Assert.That(balances, Contains.Key(new byte[] { 0x010, 0x020 }));
				Assert.That(balances, Contains.Key(new byte[] { 0x020, 0x030 }));

				Assert.That(balances[new byte[] { 0x010, 0x020 }], Is.EqualTo(10));
				Assert.That(balances[new byte[] { 0x020, 0x030 }], Is.EqualTo(100));
			}

			//var tx = new Tx();

			//tx.Hash = new byte[] { 0x01, 0x02 };
			//tx.Key = new byte[] { 0x02, 0x03 };
			//tx.TxType = TxTypeEnum.NewTx;

			//using (var dbTx = _DBContext.GetTransactionContext())
			//{
			//	_TxStore.Put(dbTx, new Keyed<Tx>(tx.Key, tx));

			//	Assert.That(_TxStore.ContainsKey(dbTx, tx.Key));
			//}
		}

		private readonly Random _Random = new Random();

		protected Keyed<Types.Transaction> GetTx(byte[] parentTx = null)
		{
		//	var nonce = new byte[10];
		//	_Random.NextBytes(nonce);

			var outpoints = new List<Types.Outpoint>();

			if (parentTx != null)
				outpoints.Add(new Types.Outpoint(parentTx, 0));

			var outputs = new List<Types.Output>();

			var address = new byte[32];
			_Random.NextBytes(address);
			var pklock = Types.OutputLock.NewPKLock(address);
			outputs.Add(new Types.Output(pklock, new Types.Spend(Consensus.Tests.zhash, (ulong)_Random.Next(100))));

			var tx = new Types.Transaction(
				0,
				ListModule.OfSeq(outpoints),
				ListModule.OfSeq(new List<byte[]>()),
				ListModule.OfSeq(outputs),
				null);

			var key = Merkle.transactionHasher.Invoke(tx);

			return new Keyed<Types.Transaction>(key, tx);
		}
	}
}
