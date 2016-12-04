using System;
using System.Collections.Generic;

namespace NodeConsole
{
	public partial class Settings {
		public string ExternalIPAddress;
		public int ServerPort;

		public override string ToString ()
		{
			return 
				$"Settings:\n" +
				$"ExternalIPAddress: {ExternalIPAddress}\n" +
				$"ServerPort: {ServerPort}";
		}
	}
}
