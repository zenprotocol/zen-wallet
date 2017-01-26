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
using System.Reflection;

namespace BlockChain.Tests
{
	public class BlockChainTestsBase
	{
		private readonly Random _Random = new Random();
		private const string DB = "temp";

		protected BlockChain _BlockChain;
		protected Keyed<Types.Block> _GenesisBlock;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			Dispose();
			_GenesisBlock = GetBlock();
			_BlockChain = new BlockChain(DB, _GenesisBlock.Key);
		}

		[OneTimeTearDown]
		public void Dispose()
		{
			if (_BlockChain != null)
			{
				_BlockChain.Dispose();
			}

			if (Directory.Exists(DB))
			{
				Directory.Delete(DB, true);
			}
		}

		protected LocationEnum Location(Keyed<Types.Block> block)
		{
			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				return _BlockChain.BlockStore.GetLocation(dbTx, block.Key);
			}
		}

		protected Keyed<Types.Block> GetBlock(Keyed<Types.Block> parent, Keyed<Types.Transaction> tx = null)
		{
			return GetBlock(parent.Key, parent.Value.header.blockNumber + 1, tx);
		}

		protected Keyed<Types.Block> GetBlock(Keyed<Types.Transaction> tx = null)
		{
			return GetBlock(null, 0, tx);
		}

		protected Keyed<Types.Block> GetBlock(byte[] parent, uint blockNumber, Keyed<Types.Transaction> tx = null)
		{
			var nonce = new byte[10];

			_Random.NextBytes(nonce);

			var header = new Types.BlockHeader(
				0,
				parent ?? new byte[] { },
				blockNumber,
				new byte[] { },
				new byte[] { },
				new byte[] { },
				ListModule.OfSeq<byte[]>(new List<byte[]>()),
				DateTime.Now.ToFileTimeUtc(),
				0,
				nonce
			);

			var txs = new List<Types.Transaction>();

			if (tx != null)
			{
				txs.Add(tx.Value);
			}

			var block = new Types.Block(header, ListModule.OfSeq<Types.Transaction>(txs));
			var key = Merkle.blockHeaderHasher.Invoke(header);

			return new Keyed<Types.Block>(key, block);
		}

		protected Keyed<Types.Transaction> GetTx(byte[] parentTx = null)
		{
			var nonce = new byte[10];
			_Random.NextBytes(nonce);

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
