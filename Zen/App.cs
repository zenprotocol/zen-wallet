using System;
using NBitcoin;
using Infrastructure;
using NBitcoinDerive;
using System.Threading;
using Wallet.core;
using System.Collections.Generic;
using System.Net;

namespace Zen
{
	public enum AppModeEnum {
		Tester,
		GUI,
		Console,
	}

	public class App
	{
//		private static App _Instance;

		public AppModeEnum? Mode { get; set; }
//		#if DEBUG
//		public Boolean LanMode { get; set; }
//		public Boolean DisableInboundMode { get; set; }
		public Boolean InitGenesisBlock { get; set; }
//		#endif

		public String BlockChainDB { get; set; }
		public List<String> Seeds { get; set; }
		public String Profile { get; set; }
		public int? PeersToFind { get; set; }
		public int? Connections { get; set; }
		public int? Port { get; set; }
		public bool SaveProfile { get; set; }

		public EndpointOptions EndpointOptions { get; set; }

		private static readonly object _lock = new object();

//		public static App Instance {
//			get {
//				lock (_lock)
//				{
//					_Instance = _Instance ?? new App();
//					return _Instance;
//				}
//			}
//		}
			
		public App ()
		{
			Mode = null;
			SaveProfile = false;
			Seeds = new List<string> ();
			PeersToFind = null;
			BlockChainDB = "blockchain_db";
			EndpointOptions = new EndpointOptions () { EndpointOption = EndpointOptions.EndpointOptionsEnum.UseUPnP };
		}

		public event Action<Network> OnInitProfile;
	
		public void Start(bool clearConsole = true) {
			if (!Mode.HasValue) {
				return;
			}

			InitProfile ();

			if (clearConsole) {
				Console.Clear ();
			}

			var blockchain = new BlockChain.BlockChain (BlockChainDB);

			if (InitGenesisBlock) {
				blockchain.HandleNewBlock (blockchain.GetGenesisBlock ().Value);
			}

			var walletManager = new WalletManager (blockchain);

			var nodeManager = new NodeManager (blockchain, EndpointOptions);

			if (Mode != null) {
				switch (Mode.Value) {
				case AppModeEnum.Console:
					Console.WriteLine("Press ENTER to stop");
					Console.ReadLine();
					nodeManager.Dispose();
					break;
				case AppModeEnum.GUI:
					Wallet.App.Instance.Start(nodeManager, walletManager);
					break;
				case AppModeEnum.Tester:
					NodeTester.MainClass.Main(nodeManager, walletManager);
					break;
				}
			}
		}

		private void InitProfile() {
			Profile = Profile ?? "default";

			if (!Profile.EndsWith (".xml")) {
				Profile += ".xml";
			}

			JsonLoader<Network>.Instance.FileName = Profile;

			foreach (String seed in Seeds) {
				if (!JsonLoader<Network>.Instance.Value.Seeds.Contains (seed)) {
					JsonLoader<Network>.Instance.Value.Seeds.Add (seed); 
				}
			}

			if (PeersToFind.HasValue) {
				JsonLoader<Network>.Instance.Value.PeersToFind = PeersToFind.Value;
			}

			if (Connections.HasValue) {
				JsonLoader<Network>.Instance.Value.PeersToFind = Connections.Value;
			}

			if (Port.HasValue) {
				JsonLoader<Network>.Instance.Value.PeersToFind = Port.Value;
			}

			if (SaveProfile) {
				JsonLoader<Network>.Instance.Save ();
			}

			Console.WriteLine ("Current profile settings:");
			Console.WriteLine (JsonLoader<Network>.Instance.Value);

			if (OnInitProfile != null) {
				OnInitProfile (JsonLoader<Network>.Instance.Value);
			}
		}

		public void SpecifyIp(String ip) {
			EndpointOptions.EndpointOption = EndpointOptions.EndpointOptionsEnum.UseSpecified; 
			EndpointOptions.SpecifiedAddress = IPAddress.Parse (ip);
		}
	}
}

