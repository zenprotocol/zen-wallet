using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using NUnit.Framework;

namespace NBitcoinDerive.Tests
{
	public class TestBlockChain : IDisposable
	{
		private readonly String _DbName;
		public BlockChain.BlockChain BlockChain { get; private set; }

		public TestBlockChain(String dbName)
		{
			_DbName = dbName;
			BlockChain = new BlockChain.BlockChain(dbName);
		}

		public void Dispose()
		{
			BlockChain.Dispose();
			Directory.Delete(_DbName, true);
		}
	}
}