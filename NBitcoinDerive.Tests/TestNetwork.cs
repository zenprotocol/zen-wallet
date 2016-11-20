using System;
using System.Collections.Generic;
using NBitcoin;
using NBitcoin.Protocol;

namespace NBitcoinDerive.Tests
{
	public class TestNetwork : Network
	{
		List<NetworkAddress> NetworkAddresses = new List<NetworkAddress>();

		public TestNetwork() : base()
		{
		}

		public int DefaultPort
		{
			get
			{
				return 1234;
			}
		}

		public IEnumerable<DNSSeedData> DNSSeeds
		{
			get
			{
				return new List<DNSSeedData>();
			}
		}

		public uint Magic
		{
			get
			{
				return 1;
			}
		}

		public IEnumerable<NetworkAddress> SeedNodes
		{
			get
			{
				return NetworkAddresses;
			}
		}

		public void AddSeed(NetworkAddress NetworkAddress)
		{
			NetworkAddresses.Add(NetworkAddress);
		}
	}
}
