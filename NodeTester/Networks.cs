//using System;
//using System.Collections.Generic;
//using Infrastructure;
//using NBitcoin;
//using NBitcoin.Protocol;
//
//namespace NodeTester
//{
//	public class TestNetwork : Singleton<TestNetwork>, Network
//	{
//		private const uint MAGIC = 1; //TODO
//		private List<NetworkAddress> seedAddreses = new List<NetworkAddress>();
//
//		public TestNetwork() : base()
//		{
//			foreach (String seed in JsonLoader<Settings>.Instance.Value.IPSeeds)
//			{
//				seedAddreses.Add(new NBitcoin.Protocol.NetworkAddress(NodeCore.Utils.ParseIPEndPoint(seed)));
//			}
//		}
//
//		public int DefaultPort
//		{
//			get
//			{
//				return 9999; //TODO: what's this for..?
//			}
//		}
//
//		public IEnumerable<DNSSeedData> DNSSeeds
//		{
//			get
//			{
//				return new List<DNSSeedData>();
//			}
//		}
//
//		public uint Magic
//		{
//			get
//			{
//				return MAGIC;
//			}
//		}
//
//		public IEnumerable<NetworkAddress> SeedNodes
//		{
//			get
//			{
//				return seedAddreses;
//			}
//		}
//	}
//}
