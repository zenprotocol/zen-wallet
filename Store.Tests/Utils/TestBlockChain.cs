using System;
using System.IO;

namespace BlockChain.Tests
{
	public class TestBlockChain : IDisposable
	{
		private readonly String _DbName;
		public BlockChain BlockChain { get; private set; }

		public TestBlockChain() : this("test_blockchain-" + new Random().Next(0, 1000))
		{
		}

		public TestBlockChain(String dbName)
		{
			_DbName = dbName;
		//	var genesisBlock = Util.GetGenesisBlock();
		//	BlockChain = new BlockChain(dbName, genesisBlock.Key);
		}

		public void Dispose()
		{
			BlockChain.Dispose();
			Directory.Delete(_DbName, true);
		}
	}
}