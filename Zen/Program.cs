using System;
using NDesk.Options;
using System.IO;
using System.Linq;
using Microsoft.FSharp.Collections;
using System.Text;
using Microsoft.FSharp.Core;
using BlockChain.Data;
using System.Threading;

namespace Zen
{
	class Program {
        enum LaunchModeEnum {
            TUI,
            GUI,
            Headless
        }

		static App app;
		static LaunchModeEnum launchMode = LaunchModeEnum.GUI;
		static bool genesis = false;
		static bool rpcServer = false;
		static bool wipe = false;

		public static void Main (string[] args)
		{
            app = new App();

			bool show_help = false;

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

				{ "m|miner", "enable miner",
					v => app.MinerEnabled = true },

				{ "r|rpc", "enable RPC",
					v => rpcServer = true },

				{ "g|genesis", "add the genesis block",
					v => genesis = true },

				{ "w|wipe db's on startup",
					v => wipe = v != null },

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

			if (launchMode == LaunchModeEnum.GUI)
			{
                Init(true); //new Thread(Init).Start();
				app.GUI(true);
			}
			else
			{
				Init(launchMode != LaunchModeEnum.TUI);
			}

            if (launchMode == LaunchModeEnum.TUI)
            {
                TUI.Start(app);
            }
		}

		static void Init(bool connect)
		{
			if (wipe)
			{
				app.ResetWalletDB();
				app.ResetBlockChainDB();
				Console.WriteLine("Databases wiped.");
			}

			app.Init();

			if (genesis)
			{
				app.AddGenesisBlock();
				Console.WriteLine("Genesis block added.");
			}

			if (rpcServer)
			{
				app.StartRPCServer();
			}

            if (connect)
            {
                app.Connect();
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