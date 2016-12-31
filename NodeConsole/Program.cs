using System;
using System.Collections.Generic;
using Infrastructure;
using System.Threading;
using Infrastructure.Console;
using System.IO;
using Infrastructure.Testing;
using System.Net;
using NodeCore;
using NBitcoin.Protocol.Behaviors;
using NBitcoin.Protocol;
using Infrastructure.Testing.Blockchain;
using NBitcoinDerive;

namespace NodeConsole
{
	public class ConsoleNode : ResourceOwner
	{
		//private byte[] genesisBlockHash = new byte[] { 0x01, 0x02 };

		public ConsoleNode()
		{
			var p = new TestTransactionPool();

			p.Add("t1", 1);
			p.Add("t2", 0);
			p.Add("t3", 0);
			p.Spend("t2", "t1", 0);

			p.Render();

			var genesisBlock = new TestBlock(p.TakeOut("t1").Value);
			var block1 = new TestBlock(p.TakeOut("t2").Value, p.TakeOut("t3").Value);
			block1.Parent = genesisBlock;

			genesisBlock.Render();
			block1.Render();


			Settings settings = JsonLoader<Settings>.Instance.Value;

			var ipEndpoint = new System.Net.IPEndPoint(IPAddress.Parse(settings.ExternalIPAddress), settings.ServerPort);

			var blockChain = new BlockChain.BlockChain("db", genesisBlock.Value.Key);

			if (blockChain.GetBlock(genesisBlock.Value.Key) == null)
			{
				Console.WriteLine("Initializing blockchain...");
				blockChain.HandleNewBlock(genesisBlock.Value.Value);
				blockChain.HandleNewBlock(block1.Value.Value);
			}
			else
			{
				Console.WriteLine("Blockchain initialized.");
			}

			OwnResource(blockChain);


			blockChain.OnAddedToMempool += transaction =>
			{
				Console.WriteLine("\n** Got Transaction **\n");
			};

			var server = new Server(this, ipEndpoint, BetaNetwork.Instance);


			var addressManagerBehavior = server.Behaviors.Find<AddressManagerBehavior>();

			var addressManager = addressManagerBehavior.AddressManager;
			addressManager.Add(
				new NetworkAddress(ipEndpoint),
				ipEndpoint.Address
			);

			addressManager.Connected (new NetworkAddress (ipEndpoint));
				
			var broadcastHubBehavior = new BroadcastHubBehavior();

			Miner miner = new Miner(blockChain);
			OwnResource(miner);


			server.Behaviors.Add(broadcastHubBehavior);
			server.Behaviors.Add(new SPVBehavior(blockChain, broadcastHubBehavior.BroadcastHub));
			server.Behaviors.Add(new MinerBehavior(miner));
			server.Behaviors.Add(new ChainBehavior(blockChain));

			server.Start();
		}
	}

	class MainClass : InteractiveConsole
	{
		public static void Main(string[] args)
		{
			Thread.GetDomain().UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
			{
				Console.WriteLine(e.ExceptionObject as Exception);
			};

			JsonLoader<Settings>.Instance.FileName = "settings.json";

			var offerDelete = new Action<JsonLoader<Settings>> (x => {
				if (YesNo("Delete exising settings file?"))
					JsonLoader<Settings>.Instance.Delete();
			});

			var printSettings = new Action<JsonLoader<Settings>> (x => {
				WriteLine(x.Value.ToString());
			});

			var save = new Action<Settings> (x => {
				JsonLoader<Settings>.Instance.Save();
			});

			while (true) {
				Console.WriteLine("Console Tester (Seed Node)");

				If<JsonLoader<Settings>> (JsonLoader<Settings>.Instance, s => !s.IsNew, printSettings, offerDelete);

				Settings settings = JsonLoader<Settings>.Instance.Value;

				If<Settings> (settings, s => string.IsNullOrEmpty(s.ExternalIPAddress), s => s.ExternalIPAddress = GetSingle<String> ("ExternalIPAddress").Value, save);
				If<Settings> (settings, s => s.ServerPort == 0, s => s.ServerPort = GetSingle<int> ("Server Port").Value, save);

//				var thread = new Thread (() => {
//					while (true) {
//						Thread.Sleep (15000);
//						WriteLine ("running. press 'Y' to stop.");
//					}
//				});

				if (YesNo("Start node?"))
				{
					var consoleNode = new ConsoleNode();
					//thread.Start();

					while (!YesNo ("Stop node?")) {
					}

					consoleNode.Dispose();
					consoleNode = null;
					//thread.Abort();
				}

				if (YesNo ("Quit?")) {
					return;
				}
			}
		}
	}
}
