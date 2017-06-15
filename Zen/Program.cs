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
        enum LaunchModeEnum {
            TUI,
            GUI,
            Headless
        }

		public static void Main (string[] args)
		{
			var app = new App();

            var launchMode = LaunchModeEnum.GUI;
			bool disableNetworking = false;
			bool show_help = false;
			bool headless = false;
			bool tui = false;
			bool rpcServer = false;

			string script = null;

			var p = new OptionSet() {
				{ "headless", "start in headless mode",
					v => launchMode = LaunchModeEnum.Headless },

				{ "t|tui", "show TUI",
					v => launchMode = LaunchModeEnum.TUI },

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

            app.Init();
			
            if (!disableNetworking)
				app.Connect();
            
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

            switch (launchMode)
            {
				case LaunchModeEnum.TUI:
                    TUI.Start(app);
					break;
				case LaunchModeEnum.GUI:
                    app.GUI();
					break;
				case LaunchModeEnum.Headless:
                    Console.WriteLine("Press any key to stop...");
                    Console.ReadKey();
                    app.Dispose();
                    Console.WriteLine("Stopped.");
					break;
			}
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