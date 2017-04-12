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

namespace Zen
{
	public class App
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
					_BlockChain = new BlockChain.BlockChain(DefaultBlockChainDB + Settings.DBSuffix, GenesisBlock.Key);

				return _BlockChain;
			}
		}

		public WalletManager WalletManager
		{
			get
			{
				if (_WalletManager == null)
					_WalletManager = new WalletManager(BlockChain_, DefaultWalletDB + Settings.DBSuffix);

				_WalletManager.OnReset -= WalletOnResetHandler; // ensure single registration
				_WalletManager.OnReset += WalletOnResetHandler;
				_WalletManager.OnItems -= WalletOnItemsHandler; // ensure single registration
				_WalletManager.OnItems += WalletOnItemsHandler;

				return _WalletManager;
			}
		}

		NodeManager NodeManager
		{
			get
			{
				if (_NodeManager == null)
					_NodeManager = new NodeManager(BlockChain_);

				return _NodeManager;
			}
		}

		public App()
		{
			Settings = new Settings();

			JsonLoader<Outputs>.Instance.FileName = "genesis_outputs.json";

			InitSettingsProfile();
		}

		bool _MinerEnabled;
		internal bool MinerEnabled {
			set
			{
				_MinerEnabled = value;

				if (_NodeManager != null)
					_NodeManager.MinerEnabled = value;
			}
		}

		internal bool AddGenesisBlock()
		{
			return AddBlock(GenesisBlock.Value);
		}

		internal bool AddBlock(Types.Block block)
		{
			return BlockChain_.HandleBlock(block) ==  BlockChain.BlockVerificationHelper.BkResultEnum.Accepted;
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

		public readonly static string DefaultBlockChainDB = "blockchain_db";
		public readonly static string DefaultWalletDB = "wallet_db";

		public event Action<Settings> OnInitSettings;
		//private ManualResetEventSlim stopEvent = new ManualResetEventSlim();
		private ManualResetEventSlim stoppedEvent = new ManualResetEventSlim();

		public void Stop() {
			//stopEvent.Set();

			if (_WalletManager != null)
			{
				_WalletManager.Dispose();
				_WalletManager = null;
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
		}

		internal void ResetDB()
		{
			Stop();

			var dbDir = ConfigurationManager.AppSettings.Get("dbDir");

			if (Directory.Exists(Path.Combine(dbDir, DefaultBlockChainDB + Settings.DBSuffix)))
				Directory.Delete(Path.Combine(dbDir, DefaultBlockChainDB + Settings.DBSuffix), true);

			if (Directory.Exists(Path.Combine(dbDir, DefaultWalletDB + Settings.DBSuffix)))
				Directory.Delete(Path.Combine(dbDir, DefaultWalletDB + Settings.DBSuffix), true);
		}

		public async Task Reconnect()
		{
			if (!Settings.DisableNetworking)
			{
				Stop();

				await NodeManager.Connect();
			}
		}

		public void GUI()
		{
			Wallet.App.Instance.Start(WalletManager, NodeManager);
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
		}
	}
}

