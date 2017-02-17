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
using Infrastructure.Testing;

namespace BlockChain
{
	public class BlockChainTestsBase
	{
		private const string DB = "temp";

		protected BlockChain _BlockChain;
		//protected Types.Transaction _GenesisTx;
		protected Types.Block _GenesisBlock;
		private byte[] _GenesisBlockHash;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

			Dispose();
		//	_GenesisTx = Infrastructure.Testing.Utils.GetTx();
			_GenesisBlock = Infrastructure.Testing.Utils.GetGenesisBlock();

			_GenesisBlockHash = Merkle.blockHeaderHasher.Invoke(_GenesisBlock.header);
			_BlockChain = new BlockChain(DB, _GenesisBlockHash);
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

		protected void Reset()
		{
			_BlockChain.Dispose();
			_BlockChain = new BlockChain(DB, _GenesisBlockHash);
		}

		protected LocationEnum Location(Types.Block block)
		{
			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				var key = Merkle.blockHeaderHasher.Invoke(block.header);
				return _BlockChain.BlockStore.GetLocation(dbTx, key);
			}
		}
	}
}
