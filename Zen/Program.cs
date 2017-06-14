using System;
using NDesk.Options;
using System.IO;
using System.Linq;
using Microsoft.FSharp.Collections;
using System.Text;
using Microsoft.FSharp.Core;

namespace Zen
{
	class Program {
		public static void Main (string[] args)
		{
			var app = new App();

			bool disableNetworking = false;
			bool show_help = false;
			bool headless = false;
			bool tui = false;
			bool rpcServer = false;

			string script = null;

			var p = new OptionSet() {
				{ "headless", "start in headless mode",
					v => headless = true },

				{ "t|tui", "show TUI",
					v => tui = true },

				{ "n|network=", "use network profile",
					v => app.SetNetwork(v) },

				{ "wallet=", "wallet DB", 
					v => app.Settings.WalletDB = v },

				{ "blockchain=", "blockchain DB suffix",
					v => app.Settings.BlockChainDBSuffix = v },

				{ "d|disable-network", "disable networking",
					v => disableNetworking = true },

				{ "s|script=", "execute script",
					v => script = v.EndsWith(".fs", StringComparison.OrdinalIgnoreCase) ? v : v + ".fs" },

				{ "m|miner", "enable miner",
					v => app.MinerEnabled = true },

				{ "r|rpc", "enable RPC",
					v => rpcServer = true },

				{ "h|help",  "show this message and exit", 
					v => show_help = v != null },
			};

			try {
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

			if (rpcServer)
			{
                app.StartRPCServer();
			}

			if (script != null)
			{
				object result;
				var isSuccess = ScriptRunner.Execute(app, Path.Combine("Scripts", script), out result);

				if (isSuccess)
				{
					Console.WriteLine(result);
				}
				else
				{
					Console.WriteLine("\nScript error.");
					Console.ReadKey();
				}
			}

			if (tui)
			{
				TUI.Start(app, script);
				return;
			}

            // only initiat connection if no init script is used
            if (!disableNetworking)
                app.Connect();

			if (headless)
			{
                Console.ReadKey();
			}
			else
			{
				app.GUI();
			}

			app.Dispose();
		}

		static void ShowHelp (OptionSet p)
		{
			Console.WriteLine ("Usage: Zen [OPTIONS]");
			Console.WriteLine ();
			Console.WriteLine ("Options:");
			p.WriteOptionDescriptions (Console.Out);
			Console.WriteLine ();
		}
	}
}