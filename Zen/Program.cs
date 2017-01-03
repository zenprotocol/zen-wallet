using System;
using System.Collections.Generic;
using NDesk.Options;
using Infrastructure;

namespace Zen
{
	class Program {
		static int verbosity;

		public static void Main (string[] args)
		{
			string profile = null;
			bool show_help = false;
			bool save = false;
			List<string> seeds = new List<string> ();
			string peers = null;
			string connections = null;
			string port = null;

			var p = new OptionSet () {
				{ "c|console", "launch the console", v => App.Instance.Mode = AppModeEnum.Console },
				{ "g|gui", "launch the wallet gui", v => App.Instance.Mode = AppModeEnum.GUI },
				{ "t|tester", "launch the tester gui", v => App.Instance.Mode = AppModeEnum.Tester },
				{ "p|profile=", "use settings profile", v => profile = v },
				{ "save", "save settings profile", v => save = v != null },
				{ "s|seed=", "use seed ip address", v => seeds.Add(v) },
				{ "peers=", "number peers to find", v => peers = v },
				{ "connections=", "number of node connections", v => connections = v },
				{ "port=", "default network port", v => port = v },
				{ "i|internal", "use internal ip", v => App.Instance.LanMode = v != null },
				{ "d|disallow", "allow inbound connections", v => App.Instance.DisableInboundMode = v != null },
//				{ "n|name=", "the {NAME} of someone to greet.",
//					v => names.Add (v) },
//				{ "r|repeat=", 
//					"the number of {TIMES} to repeat the greeting.\n" + 
//					"this must be an integer.",
//					(int v) => repeat = v },
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

			var file = profile ?? "default";
			if (!file.EndsWith (".xml")) {
				file += ".xml";
			}

			JsonLoader<NBitcoinDerive.Network>.Instance.FileName = file; 

			if (seeds.Count > 0) {
				foreach (String seed in seeds) {
					if (!JsonLoader<NBitcoinDerive.Network>.Instance.Value.Seeds.Contains (seed)) {
						JsonLoader<NBitcoinDerive.Network>.Instance.Value.Seeds.Add (seed); 
					}
				}
			}

			if (peers != null)
				JsonLoader<NBitcoinDerive.Network>.Instance.Value.PeersToFind = int.Parse(peers);

			if (connections != null)
				JsonLoader<NBitcoinDerive.Network>.Instance.Value.MaximumNodeConnection = int.Parse(connections);

			if (port != null)
				JsonLoader<NBitcoinDerive.Network>.Instance.Value.DefaultPort = int.Parse(port);

			if (save) {
				JsonLoader<NBitcoinDerive.Network>.Instance.Save ();
			}

			Console.WriteLine ("Current profile settings:");
			Console.WriteLine (JsonLoader<NBitcoinDerive.Network>.Instance.Value);

			if (show_help) {
				ShowHelp (p);
				return;
			}

//			if (extra.Count == 0) {
//				TUI.Start(null);
//				return;
//			}

			if (App.Instance.Mode != null) {
				App.Instance.Start (false);
			} else {
				TUI.Start (null);
			}


//			string message;
//			if (extra.Count > 0) {
//				message = string.Join (" ", extra.ToArray ());
//				Debug ("Using new message: {0}", message);
//			}
//			else {
//				message = "Hello {0}!";
//				Debug ("Using default message: {0}", message);
//			}
//
//			foreach (string name in names) {
//				for (int i = 0; i < repeat; ++i)
//					Console.WriteLine (message, name);
//			}
		}

		static void ShowHelp (OptionSet p)
		{
			Console.WriteLine ("Usage: Zen [OPTIONS]+ message");
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