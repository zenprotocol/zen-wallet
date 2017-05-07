using System;
using System.Collections.Generic;

namespace NodeCore
{
	public partial class Settings {
		//public List<String> DNSSeeds = new List<string>();
		public List<String> IPSeeds = new List<string>();
		public int PeersToFind;
		public int MaximumNodeConnection;
		public int ServerPort;
//		public bool AutoConfigure;

		public override string ToString ()
		{
			return 
				$"Settings:\n" +
				$"{IPSeeds.Count} Seed(s)\n" +
				$"PeersToFind: {PeersToFind}\n" +
				$"MaximumNodeConnection: {MaximumNodeConnection}\n" +
				$"ServerPort: {ServerPort}";
		}
	}
}
