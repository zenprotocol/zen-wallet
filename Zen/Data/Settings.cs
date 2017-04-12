using System;
using System.Collections.Generic;
using System.Net;
using Network;

namespace Zen.Data
{
	public class Settings
	{
		public List<Tuple<string, string>> GenesisOutputs { get; set; }
		public bool InitGenesisBlock { get; set; }
		public String DBSuffix { get; set; }
		public List<String> Keys { get; set; }
		public String NetworkProfile { get; set; }
		public String SettingsProfile { get; set; }
		public bool SaveSettings { get; set; }
		public bool DisableNetworking { get; set; }

		public Settings() {
			GenesisOutputs = new List<Tuple<string, string>>();
			Keys = new List<string>();
		}

		public void AddOutput(String output)
		{
			try
			{
//				if (!output.Contains(","))
//				{
				//	output = 
//				}
				string[] parts = output.Split(',');

				if (parts.Length == 1)
				{
					parts = new string[] { null, output };
				}

				GenesisOutputs.Add(new Tuple<string, string>(parts[0], parts[1]));
			}
			catch
			{
				Console.WriteLine("error initializing genesis outputs with: " + output);
				throw;
			}
		}

		//public void SpecifyExternalAddress(String ip)
		//{
		//	ExternalAddress = String.IsNullOrEmpty(ip) ? null : IPAddress.Parse(ip);
		//}
	}
}
