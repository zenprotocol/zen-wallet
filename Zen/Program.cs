using System;
using NDesk.Options;
using System.Net;

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

			var p = new OptionSet() {
				{ "headless", "start in headless mode",
					v => headless = true },
				{ "t|tui", "show TUI",
					v => tui = true },
				
				{ "p|profile=", "use settings profile",
					v =>  app.Settings.NetworkProfile = v },
				//{ "settings=", "use settings profile",
				//	v =>  app.Settings.SettingsProfile = v },
				//{ "k|key=", "add private key",
				//	v => app.Settings.Keys.Add(v) },
				//{ "s|save", "save network settings profile (to be used with p option)", 
				//	v => app.Settings.SaveNetworkProfile = v != null },
				//{ "save_settings", "save general settings profile (to be used with settings option)",
				//	v => app.Settings.SaveSettings = v != null },
				//{ "seed=", "use seed ip address", 
				//	v => app.Settings.Seeds.Add(v) },
				//{ "peers=", "number peers to find", 
				//	v => app.Settings.PeersToFind = int.Parse(v) },
				//{ "connections=", "number of node connections", 
				//	v => app.Settings.Connections = int.Parse(v) },
				//{ "port=", "default network port", 
				//	v => app.Settings.Port = int.Parse(v) },

				{ "d|disable-network", "disable networking",
					v => app.Settings.DisableNetworking = true },
				{ "connect=", "connect to seed's ip address",
					v => app.Settings.ConnectToSeed = IPAddress.Parse(v) },
				{ "external-ip=", "use external ip address", 
					v => app.Settings.ExternalAddress = IPAddress.Parse(v) },
				{ "localhost", "act as localhost",
					v => app.Settings.AsLocalhost = v != null },
				{ "localhost-connect", "connect to localhost",
					v => app.Settings.ConnectToLocalhost = v != null },
				
				{ "db=", "DB suffix", 
					v => app.Settings.DBSuffix = v },
				
				//{ "o|output=", "add a genesis block transaction output (address, amount)",
				//	v => app.Settings.AddOutput(v) },
				//{ "ge|genesis", "init the genesis block",
				//	v => app.Settings.InitGenesisBlock = v != null },


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
			{
				TUI.Start(app, String.Join(" ", args));
			}
			else
			{
				app.Start();

				if (!headless)
				{
					app.GUI();
					app.Stop();
				}
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