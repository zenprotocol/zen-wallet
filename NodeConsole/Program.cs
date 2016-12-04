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

namespace NodeConsole
{
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
				//Thread.Sleep (500);


				If<JsonLoader<Settings>> (JsonLoader<Settings>.Instance, s => !s.IsNew, printSettings, offerDelete);

				Settings settings = JsonLoader<Settings>.Instance.Value;

//				If<Settings> (settings, s => s.IPSeeds.Count == 0, s => s.IPSeeds = GetList<String> ("IP seed"), save);
//				If<Settings> (settings, s => s.PeersToFind == 0, s => s.PeersToFind = GetSingle<int> ("Peers to find").Value, save);
				If<Settings> (settings, s => string.IsNullOrEmpty(s.ExternalIPAddress), s => s.ExternalIPAddress = GetSingle<String> ("ExternalIPAddress").Value, save);
				If<Settings> (settings, s => s.ServerPort == 0, s => s.ServerPort = GetSingle<int> ("Server Port").Value, save);


				//Settings settings = new Settings ();
				//settings.ExternalIPAddress = "54.187.93.213";
				//settings.ServerPort = 9999;

				if (YesNo("Start node?"))
				{
					var resourceOwner = new ResourceOwner();

					var ipEndpoint = new System.Net.IPEndPoint(IPAddress.Parse(settings.ExternalIPAddress), settings.ServerPort);

					Server server = new Server (resourceOwner, ipEndpoint, BetaNetwork.Instance);

					server.Behaviors.Add (new BroadcastHubBehavior ());

					server.Start ();

					while (true) {
						Thread.Sleep (10000);
						WriteLine ("running...");
					}
				}
			}
		}
	}
}
