using System;
using System.Collections.Generic;

namespace Network
{
	public class NetworkInfo
	{
		public List<String> Seeds { get; set; }
		public int DefaultPort { get; set; }
		public uint Magic { get; set; }
		public int PeersToFind { get; set; }
		public int MaximumNodeConnection { get; set; }
		public string ExternalIPAddress { get; set; }

#if DEBUG
		public bool IsLANClient { get; set; }
		public bool IsLANHost { get; set; }
#endif

		public NetworkInfo() {
			Seeds = new List<string> ();
		}
	}
}