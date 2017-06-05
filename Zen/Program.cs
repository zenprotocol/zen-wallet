using System;
using NDesk.Options;
using System.IO;
using System.Linq;

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
			bool rpcServer = false;

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

				{ "wallet=", "wallet DB", 
					v => app.Settings.WalletDB = v },

				{ "blockchain=", "blockchain DB suffix",
					v => app.Settings.BlockChainDBSuffix = v },

				{ "d|disable-network", "disable networking",
					v => app.Settings.DisableNetworking = true },

				{ "s|script=", "execute script",
					v => script = v.EndsWith(".fs", StringComparison.OrdinalIgnoreCase) ? v : v + ".fs" },

				{ "m|miner", "enable miner",
					v => app.MinerEnabled = true },

				{ "r|rpc", "enable RPC",
					v => rpcServer = true },

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

			Server server;
			if (rpcServer)
			{
				server = new Server(app);
				server.Start();
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
					Console.WriteLine("Script error.");
					Console.ReadKey();
					//app.Dispose();
					//return;
				}
			}


 
            ///////////////////////////////////////
            /*

            var data = ContractExamples.Temp.makeData(0, 0, 0);
            var contract = "Contracts.fs";

            app.ResetBlockChainDB();
            app.AddGenesisBlock();
            app.ResetWalletDB();
            app.ActivateTestContract(contract, 10);
            app.MineBlock();
            app.Acquire(0);
			app.Acquire(1);


            Consensus.Types.Transaction tx;
            if (!app.Spend(app.GetTestContractAddress(contract), 1, app.GetTestAddress(1).Bytes, null, out tx))
				throw new Exception();

            app.MineBlock();

            int i = 0;
            for (; i < tx.outputs.Length; i++)
            {
                if (tx.outputs[i].@lock is Consensus.Types.OutputLock.ContractLock)
                    break;
            }

            var outpoint = new Consensus.Types.Outpoint(Consensus.Merkle.transactionHasher.Invoke((tx)), (uint)i);

            var data1 = new byte[] { 0x00 }.Concat(new byte[] { (byte)outpoint.index });
            data1 = data1.Concat(outpoint.txHash);
            if (!app.SendTestContract(contract, data1.ToArray()))
                throw new Exception();

            */
            /////////////////////////////////////////////////////////


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
                Console.ReadKey();
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