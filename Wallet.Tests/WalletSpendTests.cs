using NUnit.Framework;
using Consensus;
using System.Threading;
using BlockChain;
using Wallet.core.Data;
using System.Linq;
using System;
using BlockChain;
using Wallet.core;


[TestFixture()]
public class WalletSpendTests : WalletTestsBase
{
	//Key _Key;

	//[OneTimeSetUp]
	//public void OneTimeSetUp()
	//{
	//	base.OneTimeSetUp();

	//	_Key = _WalletManager.GetUnusedKey();

	//}

	//_WalletManager.Dispose();

	[SetUp]
	public void SetUp()
	{
		OneTimeSetUp();
	}

	[Test()]
	public void ShouldTransmitRawTx()
	{
		var tx = Utils.GetTx().AddOutput(Key.Create().Address, Consensus.Tests.zhash, 12);
		var bytes = Serialization.context.GetSerializer<Types.Transaction>().PackSingleObject(tx);

		Console.WriteLine(BitConverter.ToString(bytes));

		Types.Transaction txParsed;

		Assert.That(_WalletManager.Parse(bytes, out txParsed), Is.True);
		Assert.That(_WalletManager.Transmit(txParsed), Is.EqualTo(BlockChain.BlockChain.TxResultEnum.Accepted));
	}

	[Test()]
	public void ShouldImport()
	{
		var key1 = Key.Create();
		var key2 = Key.Create();
		var key3 = Key.Create();

        _BlockChain.HandleBlock(_GenesisBlock
			.AddTx(Utils.GetTx().AddOutput(key1.Address, Consensus.Tests.zhash, 12).AddOutput(key2.Address, Consensus.Tests.zhash, 13))
			.AddTx(Utils.GetTx().AddOutput(key3.Address, Consensus.Tests.zhash, 14)));

		for (var i = 0; i < 2; i++)
		{
			_WalletManager.Import(key1);
			_WalletManager.Import(key2);
			_WalletManager.Import(key3);

			Assert.That(_WalletManager.TxDeltaList.Count, Is.EqualTo(2));
			Assert.That(_WalletManager.TxDeltaList.Sum(t => t.AssetDeltas[Consensus.Tests.zhash]), Is.EqualTo(12 + 13 + 14));
		}
	}

	[Test()]
	public void ShouldSpend()
	{
		ulong initialAmount = 11;
		ulong spendAmount = 10;
		var walletMessageEvent = new AutoResetEvent(false);

		var key1 = Key.Create();

		// init genesis with a key from wallet
		var key = _WalletManager.GetUnusedKey();
		var tx = Utils.GetTx().AddOutput(key.Address, Consensus.Tests.zhash, initialAmount);

		Action<ResetEventArgs> onReset = a => {
			Console.Write("X");
		};

		Action<TxDeltaItemsEventArgs> onItems = a => { 
			Console.Write("X");
		};


		_GenesisBlock = _GenesisBlock.AddTx(tx);
		_BlockChain.HandleBlock(_GenesisBlock);


		_WalletManager.Import(key);

		Types.Transaction txNew;
		Assert.That(_WalletManager.Sign(key1.Address, Consensus.Tests.zhash, spendAmount, out txNew), Is.True);
		Assert.That(_WalletManager.Transmit(txNew), Is.EqualTo(BlockChain.BlockChain.TxResultEnum.Accepted));

		_WalletManager.OnItems += onItems;
		_WalletManager.OnReset += onReset;


		Thread.Sleep(10000);
	}


	[Test()]
	public void ShouldNotOverspendUsingConfirmedTx()
	{
		ulong initialAmount = 11;
		ulong spendAmount = 10;
		var walletMessageEvent = new AutoResetEvent(false);

		var key1 = Key.Create();
		var key2 = Key.Create();

		// init genesis with a key from wallet
		var key = _WalletManager.GetUnusedKey();
		var tx = Utils.GetTx().AddOutput(key.Address, Consensus.Tests.zhash, initialAmount);

		_GenesisBlock = _GenesisBlock.AddTx(tx);
		_BlockChain.HandleBlock(_GenesisBlock);

		_WalletManager.Import(key);

		Types.Transaction txNew;
		Assert.That(_WalletManager.Sign(key1.Address, Consensus.Tests.zhash, spendAmount, out txNew), Is.True);
		Assert.That(_WalletManager.Transmit(txNew), Is.EqualTo(BlockChain.BlockChain.TxResultEnum.Accepted));

		// mine the tx from mempool
		_BlockChain.HandleBlock(_GenesisBlock.Child().AddTx(txNew));

		//Thread.Sleep(1000);

		Assert.That(_WalletManager.Sign(key2.Address, Consensus.Tests.zhash, spendAmount, out txNew), Is.False);
	}

	[Test()]
	public void ShouldSpendMultiple()
	{
		ulong initialAmount = 11;
		ulong spendAmount = 2;
		var walletMessageEvent = new AutoResetEvent(false);

		var key1 = Key.Create();
		var key2 = Key.Create();

		// init genesis with a key from wallet
		var key = _WalletManager.GetUnusedKey();
		var tx = Utils.GetTx().AddOutput(key.Address, Consensus.Tests.zhash, initialAmount);

		_GenesisBlock = _GenesisBlock.AddTx(tx);
		_BlockChain.HandleBlock(_GenesisBlock);

		_WalletManager.Import(key);

		Types.Transaction txNew;

		Assert.That(_WalletManager.Sign(key1.Address, Consensus.Tests.zhash, spendAmount, out txNew), Is.True);
		Assert.That(_WalletManager.Transmit(txNew), Is.EqualTo(BlockChain.BlockChain.TxResultEnum.Accepted));

		//Assert.That(_BlockChain.GetUTXOSet(null).Values.Count, Is.EqualTo(2));


		//	CollectionAssert.DoesNotContain(_BlockChain.GetUTXOSet(null).Values, tx.outputs[0]);

		Thread.Sleep(1000);

		Assert.That(_WalletManager.Sign(key2.Address, Consensus.Tests.zhash, spendAmount, out txNew), Is.True);
		Assert.That(_WalletManager.Transmit(txNew), Is.EqualTo(BlockChain.BlockChain.TxResultEnum.Accepted));
	}

	[Test()]
	public void ShouldNotOverspendUsingConfirmedTxXXXXXXXXXX()
	{
		ulong initialAmount = 11;
		ulong spendAmount = 10;
		var walletMessageEvent = new AutoResetEvent(false);

	//	var key1 = Key.Create();
	//	var key2 = Key.Create();

		var key1 = Key.Create("3c/oykrHM4qPmKo4sCMGT1PVfN+SXR7vNnEO7PvoWENGjVh0wy97F9bLeygidHSuejHmcXYKAVFdD7e33yPtyQ==");
		var key2 = Key.Create("0PO1RfoZCeJsezF1Enk2t79W1HsYQywd9EZl7VVpRJL7z9wxs/ExZxMdh3Sx10GSx5OQNyhY2pYrw3JESFJo2w==");

		// init genesis with a key from wallet
		var key = _WalletManager.GetUnusedKey();
		var tx = Utils.GetTx().AddOutput(key.Address, Consensus.Tests.zhash, initialAmount);

		_GenesisBlock = _GenesisBlock.AddTx(tx);
		_BlockChain.HandleBlock(_GenesisBlock);

		Thread.Sleep(1000);


		//_WalletManager.Import();
		Types.Transaction txNew;
		Assert.That(_WalletManager.Sign(key1.Address, Consensus.Tests.zhash, spendAmount, out txNew), Is.True);
		Assert.That(_WalletManager.Transmit(txNew), Is.EqualTo(BlockChain.BlockChain.TxResultEnum.Accepted));

		var mempoolPtx = _BlockChain.memPool.TxPool.ToList()[0].Value;
		var _tx = TransactionValidation.unpoint(mempoolPtx);
		_BlockChain.HandleBlock(_GenesisBlock.Child().AddTx(_tx));
		Thread.Sleep(1000);
		_WalletManager.Import(key);

		Assert.That(_WalletManager.Sign(key2.Address, Consensus.Tests.zhash, spendAmount, out txNew), Is.False);
	//	Thread.Sleep(1000);
	}
}
