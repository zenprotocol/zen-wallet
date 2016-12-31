using System;
using System.IO;

namespace Infrastructure.Testing.Blockchain
{
	public class TestBlockChain : IDisposable
	{
		private readonly String _DbName;
		public BlockChain.BlockChain BlockChain { get; private set; }

		public TestBlockChain(byte[] genesisBlockHash) : this("test_blockchain-" + new Random().Next(0, 1000), genesisBlockHash)
		{
		}

		public TestBlockChain(String dbName, byte[] genesisBlockHash)
		{
			_DbName = dbName;
			BlockChain = new BlockChain.BlockChain(dbName, genesisBlockHash);
		}

		public void Dispose()
		{
			BlockChain.Dispose();
			Directory.Delete(_DbName, true);
		}
	}
}