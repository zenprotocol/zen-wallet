using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using Infrastructure;
using Network;

namespace Zen.Data
{
	public class Settings
	{
		public List<Tuple<string, string>> GenesisOutputs { get; set; }
		public bool InitGenesisBlock { get; set; }

		private string _DBSuffix = null;
		public String DBSuffix { get { return string.IsNullOrEmpty(_DBSuffix) ? "" : "_" + _DBSuffix; } set { _DBSuffix = value; } }

		private string _NetworkProfile;
		public String NetworkProfile { 
			get { 
				return _NetworkProfile; 
			} 
			set {
				_NetworkProfile = value;
				JsonLoader<NetworkInfo>.Instance.FileName = value + (value.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? "" : ".json");
			}
		}

		public String SettingsProfile { get; set; }
		public bool SaveSettings { get; set; }
		public bool DisableNetworking { get; set; }

		public Settings() {
			GenesisOutputs = new List<Tuple<string, string>>();

			NetworkProfile = ConfigurationManager.AppSettings.Get("network");
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
