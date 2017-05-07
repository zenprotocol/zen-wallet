//using System;
//using System.IO;

//namespace Infrastructure.Testing.Blockchain
//{
//	public class TestBlockChain : IDisposable
//	{
//		private readonly String _DbName;
//		public BlockChain.BlockChain BlockChain { get; private set; }

//		public TestBlockChain() : this("test_blockchain-" + new Random().Next(0, 1000))
//		{
//		}

//		public TestBlockChain(String dbName)
//		{
//			_DbName = dbName;
//			BlockChain = new BlockChain.BlockChain(dbName);
//		}

//		public void Dispose()
//		{
//			BlockChain.Dispose();
//			Directory.Delete(_DbName, true);
//		}
//	}
//}