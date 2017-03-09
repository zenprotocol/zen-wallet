using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using Consensus;
using BlockChain;
using Wallet.core;

public class WalletTestsBase
{
	private readonly Random _Random = new Random();
	protected const string WALLET_DB = "temp_wallet";
	protected const string BLOCKCHAIN_DB = "temp_blockchain";

	protected BlockChain.BlockChain _BlockChain;
	protected Types.Block _GenesisBlock;
	protected Types.Transaction _NewTx;
	//protected Types.Block _LastBlock;
	protected WalletManager _WalletManager;

	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		Dispose();
		_GenesisBlock = Utils.GetGenesisBlock();

		_BlockChain = new BlockChain.BlockChain(BLOCKCHAIN_DB, Merkle.blockHeaderHasher.Invoke(_GenesisBlock.header));
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
}
