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

namespace NodeConsole
{
	public class ConsoleNode : ResourceOwner
	{
		public ConsoleNode()
		{
			Settings settings = JsonLoader<Settings>.Instance.Value;

			var ipEndpoint = new System.Net.IPEndPoint(IPAddress.Parse(settings.ExternalIPAddress), settings.ServerPort);

			var blockChain = new BlockChain.BlockChain("db");
			OwnResource(blockChain);

			var server = new Server(this, ipEndpoint, BetaNetwork.Instance);

			var addressManager = new NBitcoin.Protocol.AddressManager();
			addressManager.Add(
				new NetworkAddress(ipEndpoint),
				ipEndpoint.Address
			);

			addressManager.Good (new NetworkAddress (ipEndpoint));
				
			server.Behaviors.Add(new AddressManagerBehavior (addressManager));
			server.Behaviors.Add(new BroadcastHubBehavior());
			server.Behaviors.Add(new SPVBehavior(blockChain));

			server.Start();
		}
	}

	class MainClass : InteractiveConsole
	{
		public static void Main(string[] args)
		{
			System.Threading.Thread.GetDomain().UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
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

				var thread = new Thread (() => {
					while (true) {
						Thread.Sleep (15000);
						WriteLine ("running. press 'Y' to stop.");
					}
				});

				if (YesNo("Start node?"))
				{
					var consoleNode = new ConsoleNode();
					thread.Start();

					while (!YesNo ("Stop node?")) {
					}

					consoleNode.Dispose();
					consoleNode = null;
					thread.Abort();
				}

				if (YesNo ("Quit?")) {
					return;
				}
			}
		}
	}
}
