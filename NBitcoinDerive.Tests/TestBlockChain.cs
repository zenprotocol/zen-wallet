//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Net;
//using System.Threading.Tasks;
//using NBitcoin;
//using NBitcoin.Protocol;
//using NBitcoin.Protocol.Behaviors;
//using NUnit.Framework;

//namespace NBitcoinDerive.Tests
//{
//	public class TestBlockChain : IDisposable
//	{
//		private byte[] genesisBlockHash = new byte[] { 0x01, 0x02 };
//		private readonly String _DbName;
//		public BlockChain.BlockChain BlockChain { get; private set; }

//		public TestBlockChain(String dbName)
//		{
//			_DbName = dbName;
//			BlockChain = new BlockChain.BlockChain(dbName, genesisBlockHash);
//		}

//		public void Dispose()
//		{
//			BlockChain.Dispose();
//			Directory.Delete(_DbName, true);
//		}
//	}
//}