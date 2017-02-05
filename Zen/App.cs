using System;
using Infrastructure;
using NBitcoinDerive;
using Wallet.core;
using System.Collections.Generic;
using System.Net;
using Store;
using Consensus;
using Microsoft.FSharp.Collections;
using Wallet.core.Data;
using System.Threading;
using Infrastructure.TestingGtk;
using System.IO;

namespace Zen
{
	public class App
	{
		public Settings Settings { get; set; }

		private BlockChain.BlockChain _BlockChain;
		private WalletManager _WalletManager;
		private NodeManager _NodeManager;

		//public void ResetWallet()
		//{
		//	_WalletManager.Reset();
		//}

		internal void AddGenesisBlock()
		{
			_BlockChain.HandleNewBlock(GenesisBlock.Value);
		}

		internal void ImportKey(string key)
		{
			_WalletManager.Import(Key.Create(key));
		}

		//internal void ImportWallet()
		//{
		//	_WalletManager.Import();
		//}

		public App()
		{
			Settings = new Settings()
			{
				SaveNetworkProfile = false,
				PeersToFind = null,
				BlockChainDB = "blockchain_db",
				WalletDB = "wallet_db",
				EndpointOptions = new EndpointOptions() { EndpointOption = EndpointOptions.EndpointOptionsEnum.LocalhostClient }
			};
		}

		public event Action<Network> OnInitProfile;
		public event Action<Settings> OnInitSettings;
		//private ManualResetEventSlim stopEvent = new ManualResetEventSlim();
		private ManualResetEventSlim stoppedEvent = new ManualResetEventSlim();

		public void Stop() {
			//stopEvent.Set();

			if (_WalletManager != null)
			{
				_WalletManager.Dispose();
			}

			if (_NodeManager != null)
			{
				_NodeManager.Dispose();
			}

			if (_BlockChain != null)
			{
				_BlockChain.Dispose();
			}
		}

		internal void Init()
		{
			InitNetworkProfile();

			JsonLoader<Outputs>.Instance.FileName = "genesis_outputs.json";

			InitSettingsProfile();

			_BlockChain = new BlockChain.BlockChain(Settings.BlockChainDB, GenesisBlock.Key);
			_WalletManager = new WalletManager(_BlockChain, Settings.WalletDB);
			_NodeManager = new NodeManager(_BlockChain);
		}

		public void Start()
		{
			_NodeManager.Connect(Settings.EndpointOptions);
		}

		public void GUI()
		{
			Wallet.App.Instance.Start(_WalletManager);
		}

		private Keyed<Types.Block> _GenesisBlock = null;

		public Keyed<Types.Block> GenesisBlock
		{
			get
			{
				if (_GenesisBlock == null)
				{
					var outputs = new List<Types.Output>();
					var inputs = new List<Types.Outpoint>();
					var hashes = new List<byte[]>();
					var version = (uint)1;
					var date = "2000-02-02";

					if (JsonLoader<Outputs>.Instance.IsNew)
					{
						foreach (Tuple<string, string> genesisOutputs in Settings.GenesisOutputs)
						{
							try
							{
								var key = Key.Create(genesisOutputs.Item1);
								var amount = ulong.Parse(genesisOutputs.Item2);

								JsonLoader<Outputs>.Instance.Value.Values.Add(new Output() { Key = key.ToString(), Amount = amount });

								var pklock = Types.OutputLock.NewPKLock(key.Address);
								outputs.Add(new Types.Output(pklock, new Types.Spend(Consensus.Tests.zhash, amount)));
							}
							catch
							{
								Console.WriteLine("error initializing genesis outputs with: " + genesisOutputs.Item1 + "," + genesisOutputs.Item2);
								throw;
							}
						}

						JsonLoader<Outputs>.Instance.Save();
					}
					else
					{
						foreach (var output in JsonLoader<Outputs>.Instance.Value.Values)
						{
							var key = Key.Create(output.Key);
							var amount = output.Amount;

							var pklock = Types.OutputLock.NewPKLock(key.Address);
							outputs.Add(new Types.Output(pklock, new Types.Spend(Consensus.Tests.zhash, amount)));
						}
					}

					var transaction = new Types.Transaction(version,
						ListModule.OfSeq(inputs),
						ListModule.OfSeq(hashes),
						ListModule.OfSeq(outputs),
						null);

					var transactions = new List<Types.Transaction>();
					transactions.Add(transaction);

					var blockHeader = new Types.BlockHeader(
						version,
						new byte[] { },
						0,
						new byte[] { },
						new byte[] { },
						new byte[] { },
						ListModule.OfSeq<byte[]>(new List<byte[]>()),
						DateTime.Parse(date).ToBinary(),
						1,
						new byte[] { }
					);

					var block = new Types.Block(blockHeader, ListModule.OfSeq<Types.Transaction>(transactions));
					var blockHash = Merkle.blockHeaderHasher.Invoke(blockHeader);

					_GenesisBlock = new Keyed<Types.Block>(blockHash, block);
				}

				return _GenesisBlock;
			}
		}

		private void InitNetworkProfile() {
			string file = Settings.NetworkProfile ?? "network";

			if (!file.EndsWith (".json")) {
				file += ".json";
			}

			JsonLoader<Network>.Instance.FileName = file;

			foreach (String seed in Settings.Seeds) {
				if (!JsonLoader<Network>.Instance.Value.Seeds.Contains (seed)) {
					JsonLoader<Network>.Instance.Value.Seeds.Add (seed); 
				}
			}

			if (Settings.PeersToFind.HasValue) {
				JsonLoader<Network>.Instance.Value.PeersToFind = Settings.PeersToFind.Value;
			}

			if (Settings.Connections.HasValue) {
				JsonLoader<Network>.Instance.Value.MaximumNodeConnection = Settings.Connections.Value;
			}

			if (Settings.Port.HasValue) {
				JsonLoader<Network>.Instance.Value.DefaultPort = Settings.Port.Value;
			}

			if (Settings.SaveNetworkProfile) {
				JsonLoader<Network>.Instance.Save ();
			}

			if (OnInitProfile != null) {
				OnInitProfile (JsonLoader<Network>.Instance.Value);
			}
		}

		private void InitSettingsProfile()
		{
			JsonLoader<Keys>.Instance.FileName = "keys.json";

			if (Settings.Keys.Count > 0)
			{
				foreach (var key in Settings.Keys)
				{
					JsonLoader<Keys>.Instance.Value.Values.Add(key);
				}

				JsonLoader<Keys>.Instance.Save();
			}
			else
			{
				if (!JsonLoader<Keys>.Instance.IsNew)
				{
					foreach (var key in JsonLoader<Keys>.Instance.Value.Values)
					{
						Settings.Keys.Add(key);
					}
				}
			}
			//if (Settings.SaveSettings)
			//{
			//	string file = Settings.SettingsProfile ?? "settings";

			//	if (!file.EndsWith(".xml"))
			//	{
			//		file += ".xml";
			//	}

			//	JsonLoader<Settings>.Instance.FileName = file;
			//	JsonLoader<Settings>.Instance.Value = Settings;
			//	JsonLoader<Settings>.Instance.Save();
			//}
			//else 
			//{
			//	JsonLoader<Settings>.Instance.FileName = Settings.SettingsProfile;
			//	Settings = JsonLoader<Settings>.Instance.Value;
			//}
		}
	}
}

