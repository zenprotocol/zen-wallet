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
		Server _Server = null;

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

		public void Acquire(int utxoIndex)
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

		public Address GetTestAddress(int keyIndex)
		{
			return Key.Create(JsonLoader<Keys>.Instance.Value.Values[keyIndex]).Address;
		}

		public bool Spend(Address address, ulong amount, byte[] data = null, byte[] asset = null)
		{
			address.Data = data;
            Consensus.Types.Transaction tx;

            if (asset == null)
            {
                asset = Consensus.Tests.zhash;
            }

			if (WalletManager.Sign(address, asset, amount, out tx))
			{
				return NodeManager.Transmit(tx) == BlockChain.BlockChain.TxResultEnum.Accepted;
			}

			return false;
		}

        //TODO: refactor
        public bool Spend(Address address, ulong amount, byte[] data, byte[] asset, out Types.Transaction tx)
		{
			address.Data = data;

			if (asset == null)
			{
				asset = Consensus.Tests.zhash;
			}

			if (WalletManager.Sign(address, asset, amount, out tx))
			{
				return NodeManager.Transmit(tx) == BlockChain.BlockChain.TxResultEnum.Accepted;
			}

			return false;
		}

		public bool Sign(ulong amount, out Types.Transaction tx, Address address = null)
		{
			return WalletManager.Sign(address ?? Key.Create().Address, Consensus.Tests.zhash, amount, out tx);
		}

		internal bool Transmit(Types.Transaction tx)
		{
			return NodeManager.Transmit(tx) == BlockChain.BlockChain.TxResultEnum.Accepted;
		}

		internal bool Transmit(Types.Transaction tx, out BlockChain.BlockChain.TxResultEnum result)
		{
			result = NodeManager.Transmit(tx);
			return result == BlockChain.BlockChain.TxResultEnum.Accepted;
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

		public void Stop()
		{
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

		public List<ACSItem> GetActiveContacts()
		{
			using (var dbTx = BlockChain_.GetDBTransaction())
			{
				return new ActiveContractSet().All(dbTx).Select(t => t.Item2).ToList();
			}
		}

		public async Task Reconnect()
		{
			Stop();

			if (!Settings.DisableNetworking)
			{
				await NodeManager.Connect(JsonConvert.DeserializeObject<NetworkInfo>(File.ReadAllText(Settings.NetworkProfile)));
			}
		}

		public void GUI()
		{
			Wallet.App.Instance.Start(WalletManager, NodeManager);
		}

		public void Dump()
		{
            var blockChainDumper = new BlockChainDumper(BlockChain_, WalletManager);
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

        public void PurgeAssetsCache()
        {
            var assetsDir = ConfigurationManager.AppSettings.Get("assetsDir");

            if (Directory.Exists(assetsDir))
                Directory.Delete(assetsDir, true);
        }

		public void PurgeContracts()
		{
			//var contactsDir = ConfigurationManager.AppSettings.Get("contracts");

			if (Directory.Exists("contracts"))
				Directory.Delete("contracts", true);
		}

		public byte[] GetContractCode(byte[] contractHash)
		{
			return BlockChain_.GetContractCode(contractHash);
		}

		public Tuple<UInt64, UInt64> GetTotalAssets(byte[] contractHash)
		{
			return BlockChain_.GetTotalAssets(contractHash);	
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

					var txs = new List<Types.Transaction>();

					txs.Add(new Types.Transaction(version,
						ListModule.OfSeq(inputs),
						ListModule.OfSeq(hashes),
						ListModule.OfSeq(outputs),
						  null
					));

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

					var block = new Types.Block(blockHeader, ListModule.OfSeq<Types.Transaction>(txs));
					var blockHash = Merkle.blockHeaderHasher.Invoke(blockHeader);

					_GenesisBlock = new Keyed<Types.Block>(blockHash, block);
				}

				return _GenesisBlock;
			}
		}

		public bool ActivateTestContract(string name, int blocks)
		{
			string contractCode = File.ReadAllText(Path.Combine("TestContracts", name));

			return ActivateTestContractCode(contractCode, blocks);
		}

		public bool ActivateTestContractCode(string contractCode, int blocks)
		{
			var outputs = new List<Types.Output>();
			var inputs = new List<Types.Outpoint>();
			var hashes = new List<byte[]>();
			var version = (uint)1;

			var contractHash = Merkle.innerHash(Encoding.ASCII.GetBytes(contractCode));
			var kalapasPerBlock = (ulong)(contractCode.Length * 1000 * blocks);

			outputs.Add(new Types.Output(
				Types.OutputLock.NewContractSacrificeLock(
					new Types.LockCore(0, ListModule.OfSeq(new byte[][] { }))
				),
				new Types.Spend(Tests.zhash, kalapasPerBlock)
			));

			var tx = new Types.Transaction(version,
				ListModule.OfSeq(inputs),
				ListModule.OfSeq(hashes),
				ListModule.OfSeq(outputs),
				new Microsoft.FSharp.Core.FSharpOption<Types.ExtendedContract>(
					Types.ExtendedContract.NewContract(
						new Consensus.Types.Contract(
							Encoding.ASCII.GetBytes(contractCode),
							new byte[] { },
							new byte[] { }
						)
					))
				);

            if (NodeManager.Transmit(tx) != BlockChain.BlockChain.TxResultEnum.Accepted)
            {
                return false;
            }

            return true;
		}

        public byte[] GetContractHash(string contractCode)
        {
            return Merkle.innerHash(Encoding.ASCII.GetBytes(contractCode));
        }

		public Address GetTestContractAddress(string name)
		{
			string contractCode = File.ReadAllText(Path.Combine("TestContracts", name));

            return GetTestContractAddress(Encoding.ASCII.GetBytes(contractCode));
		}

		public Address GetTestContractAddress(byte[] contractHash)
		{
			return new Address(contractHash, AddressType.Contract);
		}

        public Types.Outpoint GetFirstContractLockOutpoint(Types.Transaction tx)
        {
			int i = 0;
			for (; i < tx.outputs.Length; i++)
			{
				if (tx.outputs[i].@lock is Consensus.Types.OutputLock.ContractLock)
					break;
			}

			return new Consensus.Types.Outpoint(Consensus.Merkle.transactionHasher.Invoke((tx)), (uint)i);
		}

        public byte[] GetOutpointBytes(Types.Outpoint outpoint)
        {
            return new byte[] { (byte)outpoint.index }.Concat(outpoint.txHash).ToArray();
        }

		public bool SendTestContractTx(string name, ulong amount, byte[] data)
		{
			var address = GetTestContractAddress(name);

			//byte[] dataCombined = new byte[data.Sum(a => a.Length)];
			//int offset = 0;
			//   foreach (byte[] array in data) {
			//       System.Buffer.BlockCopy(array, 0, dataCombined, offset, array.Length);
			//       offset += array.Length;
			//   }

			address.Data = data;

			Types.Transaction tx;

			if (!WalletManager.Sign(address, Tests.zhash, amount, out tx))
				return false;

			if (NodeManager.Transmit(tx) != BlockChain.BlockChain.TxResultEnum.Accepted)
			{
				return false;
			}

            Types.Transaction autoTx;

            if (!WalletManager.SendContract(address.Bytes, Merkle.transactionHasher.Invoke(tx), out autoTx))
            {
                return false;
            }

            return NodeManager.Transmit(tx) == BlockChain.BlockChain.TxResultEnum.Accepted;
		}

        public bool SendTestContract(string name, byte[] data)
        {
            string contractCode = File.ReadAllText(Path.Combine("TestContracts", name));
            var contractHash = Merkle.innerHash(Encoding.ASCII.GetBytes(contractCode));

            Types.Transaction autoTx;

			if (!WalletManager.SendContract(contractHash, data, out autoTx))
			{
				return false;
			}

			return NodeManager.Transmit(autoTx) == BlockChain.BlockChain.TxResultEnum.Accepted;
        }

		public bool SendTestQuotedContract(byte[] contractHash, byte[] data)
		{
			Types.Transaction autoTx;

			if (!WalletManager.SendContract(contractHash, data, out autoTx))
			{
				return false;
			}

			return NodeManager.Transmit(autoTx) == BlockChain.BlockChain.TxResultEnum.Accepted;
		}

        public Types.Outpoint FindOutpoint(Address address, byte[] asset)
		{
			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				foreach (var item in _BlockChain.UTXOStore.All(dbTx, null, false))
				{
					if (!item.Item2.spend.asset.SequenceEqual(asset))
						continue;

					byte[] _address = null;

					if (item.Item2.@lock is Types.OutputLock.ContractLock)
					{
						if (address.AddressType != AddressType.Contract)
							continue;
						else
							_address = ((Types.OutputLock.ContractLock)item.Item2.@lock).contractHash;
					}

					if (item.Item2.@lock is Types.OutputLock.PKLock)
					{
						if (address.AddressType != AddressType.PK)
							continue;
						else
							_address = ((Types.OutputLock.PKLock)item.Item2.@lock).pkHash;
					}

					if (!_address.SequenceEqual(address.Bytes))
						continue;

                    return item.Item1;
				}
			}

			////TODO: handle UTXOs consumed by mempool entries
			//foreach (var item in _BlockChain.memPool.TxPool)
			//{
			//	uint i = 0;
			//	foreach (var output in item.Value.outputs)
			//	{
			//		var contractLock = output.@lock as Types.OutputLock.ContractLock;
			//		if (contractLock != null)
			//		{
			//			utxos[new Types.Outpoint(item.Key, i)] = output;
			//		}
			//		i++;
			//	}
			//}

			return null;
		}

		public Tuple<Types.Outpoint, Types.Output> FindPointedOutpoint(Address address, byte[] asset)
		{
			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				foreach (var item in _BlockChain.UTXOStore.All(dbTx, null, false))
				{
					if (!item.Item2.spend.asset.SequenceEqual(asset))
						continue;

					byte[] _address = null;

					if (item.Item2.@lock is Types.OutputLock.ContractLock)
					{
						if (address.AddressType != AddressType.Contract)
							continue;
						else
							_address = ((Types.OutputLock.ContractLock)item.Item2.@lock).contractHash;
					}

					if (item.Item2.@lock is Types.OutputLock.PKLock)
					{
						if (address.AddressType != AddressType.PK)
							continue;
						else
							_address = ((Types.OutputLock.PKLock)item.Item2.@lock).pkHash;
					}

					if (!_address.SequenceEqual(address.Bytes))
						continue;

					return new Tuple<Types.Outpoint, Types.Output>(item.Item1, item.Item2);
				}
			}

			////TODO: handle UTXOs consumed by mempool entries
			//foreach (var item in _BlockChain.memPool.TxPool)
			//{
			//  uint i = 0;
			//  foreach (var output in item.Value.outputs)
			//  {
			//      var contractLock = output.@lock as Types.OutputLock.ContractLock;
			//      if (contractLock != null)
			//      {
			//          utxos[new Types.Outpoint(item.Key, i)] = output;
			//      }
			//      i++;
			//  }
			//}

			return null;
		}

		//public void CreateQuotedTestContract(string 

	}
}

