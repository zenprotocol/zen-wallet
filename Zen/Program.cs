using System;
using System.Collections.Generic;
using NDesk.Options;
using Infrastructure;
using NBitcoinDerive;
using System.Threading;

namespace Zen
{
	class Program {
		static int verbosity;

		public static void Main (string[] args)
		{
			App app = new App();
			bool show_help = false;
			bool headless = false;
			bool tui = false;

			var p = new OptionSet () {
				{ "headless", "start in headless mode", 
					v => headless = true },
				{ "t|tui", "show TUI",
					v => tui = true },
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
				{ "ip=", "use ip address. use blank for none (to disable inbound mode)", 
					v => app.Settings.SpecifyIp(v) },
				{ "localhost-client", "run as localhost client, add localhost server as peer",
					v => app.Settings.EndpointOptions.EndpointOption = EndpointOptions.EndpointOptionsEnum.LocalhostClient },
				{ "localhost-server", "run as localhost server, don't discover peers (avoid connecting to self)",
					v => app.Settings.EndpointOptions.EndpointOption = EndpointOptions.EndpointOptionsEnum.LocalhostServer },
				{ "blockchaindb=", "BlockChain's DB name of", 
					v => app.Settings.BlockChainDB = v },
				{ "walletdb=", "Wallet's DB name",
					v => app.Settings.WalletDB = v },
				{ "o|output=", "add a genesis block transaction output (address, amount)",
					v => app.Settings.AddOutput(v) },
				{ "ge|genesis", "init the genesis block",
					v => app.Settings.InitGenesisBlock = v != null },
				//{ "v", "increase debug message verbosity",
				//	v => { if (v != null) ++verbosity; } },
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

			app.Init();

			if (tui)
				TUI.Start(app, String.Join(" ", args));

			if (!headless)
			{
				app.Start();
				app.GUI();
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