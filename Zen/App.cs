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
using System.Linq;
using BlockChain;
using Miner;

namespace Zen
{
	public class App : IDisposable
	{
		public Settings Settings { get; set; }

        Action<List<TxDelta>> _WalletOnItemsHandler;
		public Action<List<TxDelta>> WalletOnItemsHandler
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

		BlockChain.BlockChain _BlockChain = null;
		object _BlockChainSync = new object();
		WalletManager _WalletManager = null;
		NodeManager _NodeManager = null;
        Server _Server = null;
		bool _CanConnect = true;

        public MinerManager Miner { get; private set; }

		BlockChain.BlockChain AppBlockChain
		{
			get
			{
				lock (_BlockChainSync)
				{
					if (_BlockChain == null)
						_BlockChain = new BlockChain.BlockChain(BlockChainDB, GenesisBlock.Key);
				}

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
					_WalletManager = GetWalletManager(AppBlockChain);

				return _WalletManager;
			}
		}

		WalletManager GetWalletManager(BlockChain.BlockChain blockChain)
		{
			var walletManager = new WalletManager(blockChain, WalletDB);

			walletManager.OnItems -= WalletOnItemsHandler; // ensure single registration
			walletManager.OnItems += WalletOnItemsHandler;

			return walletManager;
		}

        internal void StartRPCServer()
        {
            _Server = new Server(this);
            _Server.Start();
        }

       // public List<MinerLogData> MinerLogData { get; private set; }
		
        public NodeManager NodeManager
		{
			get
			{
				if (_NodeManager == null)
				{
					_NodeManager = new NodeManager(AppBlockChain);
					//_NodeManager.Miner.Enabled = _MinerEnabled;

					//_NodeManager.Miner.OnMinedBlock -= MinedBlockHandler;
					//_NodeManager.Miner.OnMinedBlock += MinedBlockHandler;
				}

				return _NodeManager;
			}
		}

		//void MinedBlockHandler(MinerLogData minerLogData)
		//{
		//	MinerLogData.Add(minerLogData);
		//}

		public App()
		{
			Settings = new Settings();
		//	MinerLogData = new List<MinerLogData>();

			JsonLoader<Outputs>.Instance.FileName = "genesis_outputs.json";

			if (JsonLoader<Outputs>.Instance.Corrupt)
				Console.Write("Test genesis outputs json file is invalid!");
			
            JsonLoader<TestKeys>.Instance.FileName = "keys.json";

			if (JsonLoader<TestKeys>.Instance.Corrupt)
				Console.Write("Test keys json file is invalid!");
		}

        internal bool MinerEnabled { get; set; }

		public bool AddGenesisBlock()
		{
			return AddBlock(GenesisBlock.Value);
		}

        internal bool AddBlock(Consensus.Types.Block block)
		{
            return new HandleBlockAction(block).Publish().Result.BkResultEnum == BlockChain.BlockVerificationHelper.BkResultEnum.Accepted;
		}

		public void MineTestBlock()
		{
//			NodeManager.Miner.MineTestBlock();
		}

		public void SetMinerEnabled(bool enabled)
		{
            if (enabled && Miner == null)
            {
				Miner = new MinerManager(AppBlockChain, WalletManager.GetUnusedKey().Address);
				Miner.OnMined += OnMined;
			}
            else if (!enabled && Miner != null)
            {
                Miner.Dispose();
                Miner = null;
            }
		}

		public Address GetTestAddress(int keyIndex)
		{
            return Key.Create(JsonLoader<TestKeys>.Instance.Value.Values[keyIndex].Private).Address;
		}

		public async Task<bool> Spend(Address address, ulong amount, byte[] data = null, byte[] asset = null)
		{
			address.Data = data;

            if (asset == null)
            {
                asset = Tests.zhash;
            }

            var tx = WalletManager.Sign(address, asset, amount);

            if (tx != null)
			{
                return (await NodeManager.Transmit(tx)) == BlockChain.BlockChain.TxResultEnum.Accepted;
			}

			return false;
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
					"blockchain" + (string.IsNullOrWhiteSpace(Settings.BlockChainDBSuffix) ? "" : "_" + Settings.BlockChainDBSuffix)
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

        public void Init()
        {
            var wallet = WalletManager;

            SetMinerEnabled(MinerEnabled);
        }

        void OnMined(Consensus.Types.Block bk)
        {
			if (_NodeManager != null)
	            _NodeManager.Transmit(bk);
        }

		public void Stop()
		{
            //stopEvent.Set();

            if (Miner != null)
            {
                Miner.Dispose();
                Miner = null;
            }

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

			_CanConnect = true;
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

		public void ImportTestKey(string privateKey)
		{
			WalletManager.Import(Key.Create(privateKey));
		}

        public bool TestKeyImported(string privateKey)
        {
            var privateBytes = Key.Create(privateKey).Private;
            return WalletManager.GetKeys().Any(t => t.Private.SequenceEqual(privateBytes));
		}

		public async Task Connect()
		{
            if (_CanConnect)
            {
                _CanConnect = false;
                await NodeManager.Connect(JsonConvert.DeserializeObject<NetworkInfo>(File.ReadAllText(Settings.NetworkProfile)));
            }
		}

		public void GUI(bool shutdownOnClose)
		{
            if (shutdownOnClose)
                Wallet.App.Instance.OnClose += Stop;
            
			Wallet.App.Instance.Start(WalletManager, NodeManager);
		}

		public void Dump()
		{
            var blockChainDumper = new BlockChainDumper(AppBlockChain, WalletManager);
			try
			{
				blockChainDumper.Populate();
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
			if (_Server != null)
			{
				_Server.Stop();
				_Server = null;
			}

			Stop();
		}

        private Keyed<Consensus.Types.Block> _GenesisBlock = null;

		public Keyed<Consensus.Types.Block> GenesisBlock
		{
			get
			{
				if (_GenesisBlock == null)
				{
					var outputs = new List<Consensus.Types.Output>();
					var inputs = new List<Consensus.Types.Outpoint>();
					var hashes = new List<byte[]>();
					var version = (uint)1;
					var date = "2000-02-03";

                    for (var i = 0; i < JsonLoader<TestKeys>.Instance.Value.Values.Count; i++)
                    {
                        var key = Key.Create(JsonLoader<TestKeys>.Instance.Value.Values[i].Private);
                        var amount = JsonLoader<Outputs>.Instance.Value.Values.Where(t => t.TestKeyIdx == i).Select(t => t.Amount).First();

                        outputs.Add(new Consensus.Types.Output(key.Address.GetLock(), new Consensus.Types.Spend(Consensus.Tests.zhash, amount)));
                    }

					var txs = new List<Consensus.Types.Transaction>();

					txs.Add(new Consensus.Types.Transaction(
                        version,
						ListModule.OfSeq(inputs),
						ListModule.OfSeq(hashes),
						ListModule.OfSeq(outputs),
					    null
					));

					var blockHeader = new Consensus.Types.BlockHeader(
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

					var block = new Consensus.Types.Block(blockHeader, ListModule.OfSeq<Consensus.Types.Transaction>(txs));
					var blockHash = Consensus.Merkle.blockHeaderHasher.Invoke(blockHeader);

					_GenesisBlock = new Keyed<Consensus.Types.Block>(blockHash, block);
				}

				return _GenesisBlock;
			}
		}
	}
}