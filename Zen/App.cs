using System;
using Infrastructure;
using Wallet.core;
using System.Collections.Generic;
using Store;
using Consensus;
using Microsoft.FSharp.Collections;
using Wallet.core.Data;
using System.Threading;
using BlockChain.Data;
using Network;
using Zen.Data;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;
using Newtonsoft.Json;
using System.Text;
using System.Net;

namespace Zen
{
	public class App : IDisposable
	{
		public Settings Settings { get; set; }

		Action<TxDeltaItemsEventArgs> _WalletOnItemsHandler;
		public Action<TxDeltaItemsEventArgs> WalletOnItemsHandler
		{
			get
			{
				return _WalletOnItemsHandler;
			}
			set
			{
				_WalletOnItemsHandler = value;

				if (_WalletManager != null)
				{
					_WalletManager.OnItems -= WalletOnItemsHandler; // ensure single registration
					_WalletManager.OnItems += WalletOnItemsHandler;
				}
			}
		}

		Action<ResetEventArgs> _WalletOnResetHandler;
		public Action<ResetEventArgs> WalletOnResetHandler
		{
			get
			{
				return _WalletOnResetHandler;
			}
			set
			{
				_WalletOnResetHandler = value;

				if (_WalletManager != null)
				{
					_WalletManager.OnReset -= WalletOnResetHandler; // ensure single registration
					_WalletManager.OnReset += WalletOnResetHandler;
				}
			}
		}

		BlockChain.BlockChain _BlockChain = null;
		WalletManager _WalletManager = null;
		NodeManager _NodeManager = null;

		BlockChain.BlockChain BlockChain_ //TODO: refactor class name - conflicts with namespace/member
		{
			get
			{
				if (_BlockChain == null)
					_BlockChain = new BlockChain.BlockChain(BlockChainDB, GenesisBlock.Key);

				// init wallet after having initialized blockchain
				//if (_WalletManager !== null)
				//	_WalletManager = GetWalletManager(_BlockChain);

				return _BlockChain;
			}
		}

		public WalletManager WalletManager
		{
			get
			{
				if (_WalletManager == null)
					_WalletManager = GetWalletManager(BlockChain_);

				return _WalletManager;
			}
		}

		WalletManager GetWalletManager(BlockChain.BlockChain blockChain)
		{
			var walletManager = new WalletManager(blockChain, WalletDB);

			walletManager.OnReset -= WalletOnResetHandler; // ensure single registration
			walletManager.OnReset += WalletOnResetHandler;
			walletManager.OnItems -= WalletOnItemsHandler; // ensure single registration
			walletManager.OnItems += WalletOnItemsHandler;

			return walletManager;
		}

		public List<MinerLogData> MinerLogData { get; private set; }
		public NodeManager NodeManager
		{
			get
			{
				if (_NodeManager == null)
				{
					_NodeManager = new NodeManager(BlockChain_);
					_NodeManager.Miner.Enabled = _MinerEnabled;

					_NodeManager.Miner.OnMinedBlock -= MinedBlockHandler;
					_NodeManager.Miner.OnMinedBlock += MinedBlockHandler;
				}

				return _NodeManager;
			}
		}

		void MinedBlockHandler(MinerLogData minerLogData)
		{
			MinerLogData.Add(minerLogData);
		}

		public App()
		{
			Settings = new Settings();
			MinerLogData = new List<MinerLogData>();

			JsonLoader<Outputs>.Instance.FileName = "genesis_outputs.json";
			JsonLoader<Keys>.Instance.FileName = "keys.json";
		}

		bool _MinerEnabled;
		internal bool MinerEnabled
		{
			set
			{
				_MinerEnabled = value;

				if (_NodeManager != null)
					_NodeManager.Miner.Enabled = value;
			}
		}

		public bool AddGenesisBlock()
		{
			return AddBlock(GenesisBlock.Value);
		}

		internal bool AddBlock(Types.Block block)
		{
			return BlockChain_.HandleBlock(block) == BlockChain.BlockVerificationHelper.BkResultEnum.Accepted;
		}

		public void Acuire(int utxoIndex)
		{
			WalletManager.Import(Key.Create(JsonLoader<Outputs>.Instance.Value.Values[utxoIndex].Key));
		}

		public void MineBlock()
		{
			NodeManager.Miner.Mine();
		}

		public void SetMinerEnabled(bool enabled)
		{
			NodeManager.Miner.Enabled = enabled;
		}

		public bool Spend(int amount, int keyIndex)
		{
			//TODO: handle return value
			return Spend((ulong) amount, Key.Create(JsonLoader<Keys>.Instance.Value.Values[keyIndex]).Address);
		}

		internal bool Spend(ulong amount, Address address = null)
		{
			Types.Transaction tx;

			if (WalletManager.Sign(address ?? Key.Create().Address, Consensus.Tests.zhash, amount, out tx))
			{
				return NodeManager.Transmit(tx) == BlockChain.BlockChain.TxResultEnum.Accepted;
			}

			return false;
		}

		internal bool Sign(ulong amount, out Types.Transaction tx, Address address = null)
		{
			return WalletManager.Sign(address ?? Key.Create().Address, Consensus.Tests.zhash, amount, out tx);
		}

		internal bool Transmit(Types.Transaction tx)
		{
			return NodeManager.Transmit(tx) == BlockChain.BlockChain.TxResultEnum.Accepted;
		}

		internal long AssetMount()
		{
			long amount = 0;

			WalletManager.TxDeltaList.ForEach((obj) =>
			{
				if (obj.TxState != TxStateEnum.Invalid && obj.AssetDeltas.ContainsKey(Consensus.Tests.zhash))
				{
					amount += obj.AssetDeltas[Consensus.Tests.zhash];
				}
			});

			return amount;
		}

		internal void CloseGUI()
		{
			Wallet.App.Instance.Quit();
		}

		//internal void ImportWallet()
		//{
		//	_WalletManager.Import();
		//}

		string Setting(string key)
		{
			return ConfigurationManager.AppSettings.Get(key);
		}

		public void SetBlockChainDBSuffix(string value)
		{
			Settings.BlockChainDBSuffix = value;
		}

		string BlockChainDB
		{
			get
			{
				return Path.Combine(
					Setting("dbDir"), 
					"blockchain" + (string.IsNullOrWhiteSpace(Settings.BlockChainDBSuffix) ? "" : Settings.BlockChainDBSuffix)
				);
			}
		}

		string WalletDB
		{
			get
			{
				return Path.Combine(
					Setting("dbDir"), 
					"wallets", 
					string.IsNullOrWhiteSpace(Settings.WalletDB) ? Setting("walletDb") : Settings.WalletDB
				);
			}
		}

		public event Action<Settings> OnInitSettings;
		//private ManualResetEventSlim stopEvent = new ManualResetEventSlim();
		private ManualResetEventSlim stoppedEvent = new ManualResetEventSlim();

		public void Stop() {
			//stopEvent.Set();

			if (_NodeManager != null)
			{
				_NodeManager.Dispose();
				_NodeManager = null;
			}

			if (_BlockChain != null)
			{
				_BlockChain.Dispose();
				_BlockChain = null;
			}

			if (_WalletManager != null)
			{
				_WalletManager.Dispose();
				_WalletManager = null;
			}
		}

		public void ResetBlockChainDB()
		{
			Stop();

			if (Directory.Exists(BlockChainDB))
				Directory.Delete(BlockChainDB, true);
		}

		public void ResetWalletDB()
		{
			if (_WalletManager != null)
			{
				_WalletManager.Dispose();
				_WalletManager = null;
			}

			if (Directory.Exists(WalletDB))
				Directory.Delete(WalletDB, true);
		}

		public void SetWallet(string walletDB)
		{
			if (_WalletManager != null)
			{
				_WalletManager.Dispose();
				_WalletManager = null;
			}

			Settings.WalletDB = walletDB;

			var x = WalletManager;
		}

		public void SetNetwork(string networkProfile)
		{
			Stop();

			Settings.NetworkProfile = networkProfile;
		}

		public void AddKey(int keyIndex)
		{
			WalletManager.Import(Key.Create(JsonLoader<Keys>.Instance.Value.Values[keyIndex]));
		}

		public async Task Reconnect()
		{
			if (!Settings.DisableNetworking)
			{
				Stop();

				await NodeManager.Connect(JsonConvert.DeserializeObject<NetworkInfo>(File.ReadAllText(Settings.NetworkProfile)));
			}
		}

		public void GUI()
		{
			Wallet.App.Instance.Start(WalletManager, NodeManager);
		}

		public void Dump()
		{
			var blockChainDumper = new BlockChainDumper(BlockChain_);
			try
			{
				blockChainDumper.Populate(WalletManager, BlockChain_);
				var jsonString = blockChainDumper.Generate();

				File.WriteAllText("nodes.js", "var graph = " + jsonString);

				string path = Directory.GetCurrentDirectory();
				System.Diagnostics.Process.Start(Path.Combine(path, "graph.html"));
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
		//		if (File.Exists("nodes.js"))
		//			File.Delete("nodes.js");
			}
		}

		public void Dispose()
		{
			Stop();
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

								outputs.Add(new Types.Output(key.Address.GetLock(), new Types.Spend(Consensus.Tests.zhash, amount)));
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

							outputs.Add(new Types.Output(key.Address.GetLock(), new Types.Spend(Consensus.Tests.zhash, amount)));
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
						//DateTime.Now.ToBinary(),
						DateTime.Parse(date).Ticks,
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
	}
}

