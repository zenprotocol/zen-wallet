using System;
using System.IO;
using Consensus;
using NUnit.Framework;
using Store;
using Wallet.core;

namespace Wallet.Tests
{
	public class TestBase
	{
		private string _BlockChainDB = "test-blockchain";
		private string _WalletDB = "test-wallet";

		protected void TestAction(Keyed<Types.Block> genesisBlock, Action<WalletManager, BlockChain.BlockChain> action)
		{
			TearDown();

			var blockChain = new BlockChain.BlockChain(_BlockChainDB, genesisBlock.Key);
			var walletManager = new WalletManager(blockChain, _WalletDB);

			try
			{
				action(walletManager, blockChain);
			}
			catch (Exception e)
			{
				Assert.That(e, Is.Empty);
			}

			TearDown();
		}

		private void TearDown()
		{
			DeleteDirectory(_BlockChainDB);
			DeleteDirectory(_WalletDB);
		}

		private void DeleteDirectory(string directory)
		{
			if (Directory.Exists(directory))
			{
				Directory.Delete(directory, true);
			}
		}
	}
}
