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

		private BlockChain.BlockChain _BlockChain;
		private WalletManager _WalletManager;
		private NodeManager _NodeManager;

		public App()
		{
			Settings = new Settings();

			string networkProfileFile = Settings.NetworkProfile ?? ConfigurationManager.AppSettings.Get("network");

			if (!networkProfileFile.EndsWith(".json"))
			{
				networkProfileFile += ".json";
			}

			JsonLoader<NetworkInfo>.Instance.FileName = networkProfileFile;

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
			EnsureInitialized(_BlockChain);
			return _BlockChain.HandleBlock(block) == BlockChain.BlockVerificationHelper.BkResultEnum.Accepted;
		}

		internal void ImportKey(string key)
		{
			EnsureInitialized(_WalletManager);
			_WalletManager.Import(Key.Create(key));
		}

		internal Key GetUnusedKey()
		{
			EnsureInitialized(_WalletManager);
			return _WalletManager.GetUnusedKey();
		}

		internal bool Spend(ulong amount)
		{
			Types.Transaction tx;
			return Spend(amount, out tx);
		}

		internal bool Spend(ulong amount, out Types.Transaction tx)
		{
			var key = Key.Create();

			if (_WalletManager.Sign(key.Address, Consensus.Tests.zhash, amount, out tx))
			{
				return _NodeManager.Transmit(tx) == BlockChain.BlockChain.TxResultEnum.Accepted;
			}
			else
			{
				return false;
			}
		}

		internal bool Sign(ulong amount, out Types.Transaction tx)
		{
			var key = Key.Create();

			return _WalletManager.Sign(key.Address, Consensus.Tests.zhash, amount, out tx);
		}

		internal bool Transmit(Types.Transaction tx)
		{
			return _NodeManager.Transmit(tx) == BlockChain.BlockChain.TxResultEnum.Accepted;
		}

		internal long AssetMount()
		{
			long amount = 0;

			_WalletManager.TxDeltaList.ForEach((obj) =>
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
			Directory.Delete(ConfigurationManager.AppSettings.Get("dbDir"), true);
		}

		internal void EnsureInitialized(object obj = null)
		{
			if (_BlockChain == null)
				_BlockChain = new BlockChain.BlockChain(DefaultBlockChainDB + Settings.DBSuffix, GenesisBlock.Key);

			if (obj == _BlockChain)
				return;

			if (_WalletManager == null)
				_WalletManager = new WalletManager(_BlockChain, DefaultWalletDB + Settings.DBSuffix);

			if (obj == _WalletManager)
				return;

			if (_NodeManager == null)
			{
				_NodeManager = new NodeManager(_BlockChain);
				_NodeManager.MinerEnabled = _MinerEnabled;
			}
		}

		public async Task Reconnect()
		{
			if (!Settings.DisableNetworking)
			{
				Stop();
				EnsureInitialized();

				await _NodeManager.Connect();
			}
		}

		public void GUI()
		{
			EnsureInitialized();
			Wallet.App.Instance.Start(_WalletManager, _NodeManager);
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

