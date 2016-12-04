using System;
using System.Collections.Generic;

namespace NodeTester
{
	public partial class Settings {
		//public List<String> DNSSeeds = new List<string>();
		public List<String> IPSeeds = new List<string>();
		public int PeersToFind;
		public int MaximumNodeConnection;
		public int ServerPort;
		public bool AutoConfigure;
		public bool DowngradeToLAN;
	}
}
