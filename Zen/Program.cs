using System;
using NDesk.Options;
using System.IO;

namespace Zen
{
	class Program {
		static int verbosity;

		public static void Main (string[] args)
		{
			var app = new App();

			bool show_help = false;
			bool headless = false;
			bool tui = false;
			string script = null;

			var p = new OptionSet() {
				{ "headless", "start in headless mode",
					v => headless = true },
				{ "t|tui", "show TUI",
					v => tui = true },
				
				{ "n|network=", "use network profile",
					v =>  app.Settings.NetworkProfile = v },
				//{ "settings=", "use settings profile",
				//	v =>  app.Settings.SettingsProfile = v },
				//{ "k|key=", "add private key",
				//	v => app.Settings.Keys.Add(v) },
				//{ "s|save", "save network settings profile (to be used with p option)", 
				//	v => app.Settings.SaveNetworkProfile = v != null },
				//{ "save_settings", "save general settings profile (to be used with settings option)",
				//	v => app.Settings.SaveSettings = v != null },

				{ "wallet=", "specify wallet", 
					v => app.Settings.WalletDB = v },

				{ "db=", "Wallet DB",
					v => app.Settings.WalletDB = v },

				{ "d|disable-network", "disable networking",
					v => app.Settings.DisableNetworking = true },

				{ "s|script=", "execute script",
					v => script = v.EndsWith(".fs", StringComparison.OrdinalIgnoreCase) ? v : v + ".fs" },

				{ "m|miner", "enable miner",
					v => app.MinerEnabled = true },

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

			if (script != null)
			{
				object result;
				var isSuccess = ScriptRunner.Execute(app, Path.Combine("Scripts", script), out result);

				if (isSuccess)
				{
					Console.WriteLine(result);
					Console.ReadKey();
				}
				else
				{
					Console.ReadKey();
					app.Dispose();
					return;
				}
			}

			if (tui)
			{
				TUI.Start(app, script);
				return;
			}

			if (!headless)
			{
				app.GUI();
				app.Dispose();
			}
			else
			{
				app.Reconnect();
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