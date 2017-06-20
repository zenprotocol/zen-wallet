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
		public bool InitGenesisBlock { get; set; }

		public string WalletDB { get; set; }
		public string BlockChainDBSuffix { get; set; }

		private string _NetworkProfile;
		public String NetworkProfile { 
			get { 
				return _NetworkProfile; 
			} 
			set {
				_NetworkProfile = value + (value.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? "" : ".json");
			}
		}

		public String SettingsProfile { get; set; }

		public Settings() {
			WalletDB = null;
			NetworkProfile = ConfigurationManager.AppSettings.Get("network");
		}
	}
}
