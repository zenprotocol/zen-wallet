using NUnit.Framework;
using Consensus;
using System.Threading;
using Infrastructure.Testing;
using BlockChain;
using Wallet.core.Data;
using System.Linq;
using System;

namespace Wallet.core
{
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
		public void ShouldSpend()
		{
			ulong initialAmount = 11;
			ulong spendAmount = 10;
			var walletMessageEvent = new AutoResetEvent(false);

			var key1 = Key.Create();

			// init genesis with a key from wallet
			var key = _WalletManager.GetUnusedKey();
			var tx = Utils.GetTx().AddOutput(key.Address, Tests.zhash, initialAmount);

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
			Assert.That(_WalletManager.Sign(key1.Address, Tests.zhash, spendAmount, out txNew), Is.True);
			Assert.That(_WalletManager.Transmit(txNew), Is.True);

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
			var tx = Utils.GetTx().AddOutput(key.Address, Tests.zhash, initialAmount);

			_GenesisBlock = _GenesisBlock.AddTx(tx);
			_BlockChain.HandleBlock(_GenesisBlock);

			_WalletManager.Import(key);

			Types.Transaction txNew;
			Assert.That(_WalletManager.Sign(key1.Address, Tests.zhash, spendAmount, out txNew), Is.True);
			Assert.That(_WalletManager.Transmit(txNew), Is.True);

			// mine the tx from mempool
			_BlockChain.HandleBlock(_GenesisBlock.Child().AddTx(txNew));

			//Thread.Sleep(1000);

			Assert.That(_WalletManager.Sign(key2.Address, Tests.zhash, spendAmount, out txNew), Is.False);
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
			var tx = Utils.GetTx().AddOutput(key.Address, Tests.zhash, initialAmount);

			_GenesisBlock = _GenesisBlock.AddTx(tx);
			_BlockChain.HandleBlock(_GenesisBlock);

			_WalletManager.Import(key);

			Types.Transaction txNew;

			Assert.That(_WalletManager.Sign(key1.Address, Tests.zhash, spendAmount, out txNew), Is.True);
			Assert.That(_WalletManager.Transmit(txNew), Is.True);

			//Assert.That(_BlockChain.GetUTXOSet(null).Values.Count, Is.EqualTo(2));


			//	CollectionAssert.DoesNotContain(_BlockChain.GetUTXOSet(null).Values, tx.outputs[0]);

			Thread.Sleep(1000);

			Assert.That(_WalletManager.Sign(key2.Address, Tests.zhash, spendAmount, out txNew), Is.True);
			Assert.That(_WalletManager.Transmit(txNew), Is.True);
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
			var tx = Utils.GetTx().AddOutput(key.Address, Tests.zhash, initialAmount);

			_GenesisBlock = _GenesisBlock.AddTx(tx);
			_BlockChain.HandleBlock(_GenesisBlock);

			Thread.Sleep(1000);


			//_WalletManager.Import();
			Types.Transaction txNew;
			Assert.That(_WalletManager.Sign(key1.Address, Tests.zhash, spendAmount, out txNew), Is.True);
			Assert.That(_WalletManager.Transmit(txNew), Is.True);

			var mempoolPtx = _BlockChain.pool.Transactions.ToList()[0].Value;
			var _tx = TransactionValidation.unpoint(mempoolPtx);
			_BlockChain.HandleBlock(_GenesisBlock.Child().AddTx(_tx));
			Thread.Sleep(1000);
			_WalletManager.Import(key);

			Assert.That(_WalletManager.Sign(key2.Address, Tests.zhash, spendAmount, out txNew), Is.False);
		//	Thread.Sleep(1000);
		}
	}
}
