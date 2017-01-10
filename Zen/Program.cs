using System;
using System.Collections.Generic;
using NDesk.Options;
using Infrastructure;
using NBitcoinDerive;

namespace Zen
{
	class Program {
		static int verbosity;

		public static void Main (string[] args)
		{
			App app = new App();
			bool show_help = false;
		
			var p = new OptionSet () {
				{ "c|console", "launch the console", 
					v => app.Settings.Mode = Settings.AppModeEnum.Console },
				{ "g|gui", "launch the wallet gui", 
					v => app.Settings.Mode = Settings.AppModeEnum.GUI },
				{ "tester", "launch the tester gui", 
					v => app.Settings.Mode = Settings.AppModeEnum.Tester },
				{ "p|profile=", "use settings profile",
					v =>  app.Settings.NetworkProfile = v },
				{ "settings=", "use settings profile",
					v =>  app.Settings.SettingsProfile = v },
				{ "k|key=", "add private key",
					v => app.Settings.Keys.Add(v) },
				{ "s|save", "save network settings profile (to be used with p option)", 
					v => app.Settings.SaveNetworkProfile = v != null },
				{ "save_settings", "save general settings profile (to be used with settings option)",
					v => app.Settings.SaveSettings = v != null },
				{ "seed=", "use seed ip address", 
					v => app.Settings.Seeds.Add(v) },
				{ "peers=", "number peers to find", 
					v => app.Settings.PeersToFind = int.Parse(v) },
				{ "connections=", "number of node connections", 
					v => app.Settings.Connections = int.Parse(v) },
				{ "port=", "default network port", 
					v => app.Settings.Port = int.Parse(v) },
				{ "ip=", "use ip address. use blank for none", 
					v => app.Settings.SpecifyIp(v) },
				{ "i|internal", "use internal ip", 
					v => app.Settings.EndpointOptions.EndpointOption = EndpointOptions.EndpointOptionsEnum.UseInternalIP },
				{ "blockchaindb=", "BlockChain's DB name of", 
					v => app.Settings.BlockChainDB = v },
				{ "walletdb=", "Wallet's DB name",
					v => app.Settings.BlockChainDB = v },
				{ "o|output=", "add a genesis block transaction output (address, amount)",
					v => app.Settings.AddOutput(v) },
				{ "ge|genesis", "init the genesis block",
					v => app.Settings.InitGenesisBlock = v != null },
				{ "v", "increase debug message verbosity",
					v => { if (v != null) ++verbosity; } },
				{ "h|help",  "show this message and exit", 
					v => show_help = v != null },
			};

			//List<string> extra;
			try {
				//extra = 
				p.Parse (args);
			}
			catch (OptionException e) {
				Console.Write ("greet: ");
				Console.WriteLine (e.Message);
				Console.WriteLine ("Try `greet --help' for more information.");
				return;
			}

			if (show_help) {
				ShowHelp (p);
				return;
			}

			try
			{
				app.Init();

				if (app.Settings.Mode.HasValue)
				{
					app.Start(false);
					return;
				}

				TUI.Start(app, String.Join(" ", args));
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}

		static void ShowHelp (OptionSet p)
		{
			Console.WriteLine ("Usage: Zen [OPTIONS]");
			Console.WriteLine ("Description");
			Console.WriteLine ();
			Console.WriteLine ("Options:");
			p.WriteOptionDescriptions (Console.Out);
			Console.WriteLine ();
			Console.WriteLine ("Examples:");
			Console.WriteLine (" Run using internal IP but don't act as server (to be used for testing purposes, when several nodes on a single machine)");
			Console.WriteLine ();
			Console.WriteLine ("mono --debug Zen.exe --internal --ip=");
			Console.WriteLine ();
		}

		static void Debug (string format, params object[] args)
		{
			if (verbosity > 0) {
				Console.Write ("# ");
				Console.WriteLine (format, args);
			}
		}
	}
}