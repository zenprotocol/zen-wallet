using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using Store;
using Consensus;
using Microsoft.FSharp.Collections;
using System.Collections.Generic;
using Wallet.core.Data;
using System.Threading;
using System.Threading.Tasks;
using Infrastructure;

namespace Wallet.core
{
	[TestFixture()]
	public class WalletTests
	{
		private readonly Random _Random = new Random();
		private const string WALLET_DB = "temp_wallet";
		private const string BLOCKCHAIN_DB = "temp_blockchain";

		protected BlockChain.BlockChain _BlockChain;
		protected Keyed<Types.Block> _GenesisBlock;
	//	protected Types.Transaction _GenesisTx;
		protected WalletManager _WalletManager;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			Dispose();
			_GenesisBlock = GetBlock();

		//	_GenesisTx = GetTx(null, Key.Create().Address, 100).Value;
			_BlockChain = new BlockChain.BlockChain(BLOCKCHAIN_DB, _GenesisBlock.Key);
			_WalletManager = new WalletManager(_BlockChain, WALLET_DB);
		}

		[OneTimeTearDown]
		public void Dispose()
		{
			if (_WalletManager != null)
			{
				_WalletManager.Dispose();
			}

			if (_BlockChain != null)
			{
				_BlockChain.Dispose();
			}

			if (Directory.Exists(BLOCKCHAIN_DB))
			{
				Directory.Delete(BLOCKCHAIN_DB, true);
			}

			if (Directory.Exists(WALLET_DB))
			{
				Directory.Delete(WALLET_DB, true);
			}
		}

		[Test(), Order(1)]
		public void ShouldShowBalances()
		{
			var _event = new AutoResetEvent(false);
			var tx = GetTx(null, _WalletManager.GetUnusedKey().Address, 10);
			var block = GetBlock(_GenesisBlock, tx);

			_BlockChain.HandleNewBlock(_GenesisBlock.Value);

			MessageProducer<IWalletMessage>.Instance.AddMessageListener(new MessageListener<IWalletMessage>(i => { 
				Assert.That(i, Is.InstanceOf(typeof(ResetMessage)));
				Assert.That(((ResetMessage)i)[0].Balances[Tests.zhash] == 10, Is.True);
				_event.Set();
			}));

			_WalletManager.Import();
			_BlockChain.HandleNewBlock(block.Value);

			Assert.That(_event.WaitOne(30000), Is.True);
		}

		[Test(), Order(2)]
		public void ShouldLoadWalletDB()
		{
			_WalletManager.Dispose();

			_WalletManager = new WalletManager(_BlockChain, WALLET_DB);

			Assert.That(_WalletManager.WalletBalances[0].Balances[Tests.zhash] == 10, Is.True);
		}


//		TODO: test: are keys marked as used during spend?

		/////////////////////////////////////

		protected Keyed<Types.Block> GetBlock(Keyed<Types.Block> parent, Keyed<Types.Transaction> tx = null)
		{
			return GetBlock(parent.Key, parent.Value.header.blockNumber + 1, tx);
		}

		protected Keyed<Types.Block> GetBlock(Keyed<Types.Transaction> tx = null)
		{
			return GetBlock(null, 0, tx);
		}

		protected Keyed<Types.Block> GetBlock(byte[] parent, uint blockNumber, Keyed<Types.Transaction> tx = null)
		{
			var nonce = new byte[10];

			_Random.NextBytes(nonce);

			var header = new Types.BlockHeader(
				0,
				parent ?? new byte[] { },
				blockNumber,
				new byte[] { },
				new byte[] { },
				new byte[] { },
				ListModule.OfSeq<byte[]>(new List<byte[]>()),
				DateTime.Now.ToFileTimeUtc(),
				0,
				nonce
			);

			var txs = new List<Types.Transaction>();

			if (tx != null)
			{
				txs.Add(tx.Value);
			}

			var block = new Types.Block(header, ListModule.OfSeq<Types.Transaction>(txs));
			var key = Merkle.blockHeaderHasher.Invoke(header);

			return new Keyed<Types.Block>(key, block);
		}

		protected Keyed<Types.Transaction> GetTx(byte[] parentTx, byte[] address, ulong amount)
		{
			var outpoints = new List<Types.Outpoint>();

			if (parentTx != null)
				outpoints.Add(new Types.Outpoint(parentTx, 0));

			var outputs = new List<Types.Output>();

			var pklock = Types.OutputLock.NewPKLock(address);
			outputs.Add(new Types.Output(pklock, new Types.Spend(Tests.zhash, amount)));

			var tx = new Types.Transaction(
				0,
				ListModule.OfSeq(outpoints),
				ListModule.OfSeq(new List<byte[]>()),
				ListModule.OfSeq(outputs),
				null);

			var key = Merkle.transactionHasher.Invoke(tx);

			return new Keyed<Types.Transaction>(key, tx);
		}

	}
}



