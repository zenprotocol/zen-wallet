using System;
using System.Collections.Generic;
using System.Net;
using NBitcoinDerive;

namespace Zen
{
	public class Settings
	{
		public enum AppModeEnum
		{
			Tester,
			GUI,
			Console,
		}

		public AppModeEnum? Mode { get; set; }
		public List<Tuple<string, string>> GenesisOutputs { get; set; }
		public bool InitGenesisBlock { get; set; }
		public String BlockChainDB { get; set; }
		public String WalletDB { get; set; }
		public List<String> Seeds { get; set; }
		public List<String> Keys { get; set; }
		public String NetworkProfile { get; set; }
		public String SettingsProfile { get; set; }
		public int? PeersToFind { get; set; }
		public int? Connections { get; set; }
		public int? Port { get; set; }
		public bool SaveNetworkProfile { get; set; }
		public bool SaveSettings { get; set; }
		public EndpointOptions EndpointOptions { get; set; }

		public Settings() {
			Seeds = new List<string>();
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

		public void SpecifyIp(String ip)
		{
			if (String.IsNullOrEmpty(ip))
			{
				EndpointOptions.EndpointOption = EndpointOptions.EndpointOptionsEnum.UseNone;
			}
			else
			{
				EndpointOptions.EndpointOption = EndpointOptions.EndpointOptionsEnum.UseSpecified;
				EndpointOptions.SpecifiedAddress = IPAddress.Parse(ip);
			}
		}
	}
}
