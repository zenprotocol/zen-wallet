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
				{ "console", "launch the console", 
					v => app.Mode = AppModeEnum.Console },
				{ "gui", "launch the wallet gui", 
					v => app.Mode = AppModeEnum.GUI },
				{ "tester", "launch the tester gui", 
					v => app.Mode = AppModeEnum.Tester },
				{ "p|profile=", "use settings profile", 
					v =>  app.Profile = v },
				{ "save", "save settings profile (to be used with p option)", 
					v => app.SaveProfile = v != null },
				{ "seed=", "use seed ip address", 
					v => app.Seeds.Add(v) },
				{ "peers=", "number peers to find", 
					v => app.PeersToFind = int.Parse(v) },
				{ "connections=", "number of node connections", 
					v => app.Connections = int.Parse(v) },
				{ "port=", "default network port", 
					v => app.Port = int.Parse(v) },
				{ "ip=", "use ip address. use blank for none", 
					v => app.SpecifyIp(v) },
				{ "internal", "use internal ip", 
					v => app.EndpointOptions.EndpointOption = 
						EndpointOptions.EndpointOptionsEnum.UseInternalIP },
				{ "bcdb=", "DB name of BlockChain", 
					v => app.BlockChainDB = v },
				{ "genesis", "DB name of BlockChain", 
					v => app.InitGenesisBlock = v != null },
				{ "v", "increase debug message verbosity",
					v => { if (v != null) ++verbosity; } },
				{ "h|help",  "show this message and exit", 
					v => show_help = v != null },
			};

			List<string> extra;
			try {
				extra = p.Parse (args);
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

			if (app.Mode.HasValue) {
				app.Start (false);
				return;
			}

//			if (extra.Count == 0) {
			TUI.Start (app);
		}

		static void ShowHelp (OptionSet p)
		{
			Console.WriteLine ("Usage: Zen [OPTIONS]");
			Console.WriteLine ("Description");
			Console.WriteLine ();
			Console.WriteLine ("Options:");
			p.WriteOptionDescriptions (Console.Out);
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