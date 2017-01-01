using System;
using System.Collections.Generic;

namespace NBitcoinDerive
{
	public class Network
	{
		public List<String> Seeds { get; set; }
		public int DefaultPort { get; set; }
		public uint Magic { get; }
		public int PeersToFind;
		public int MaximumNodeConnection;

		public Network() {
			Seeds = new List<string> ();
		}
	}
}
