using NUnit.Framework;
using Consensus;
using System.Threading;
using Infrastructure.Testing;
using BlockChain;
using Wallet.core.Data;

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
			_BlockChain.HandleNewBlock(_GenesisBlock);

			_WalletManager.Import(key);

			var txNew = _WalletManager.Sign(key1.Address, Tests.zhash, spendAmount);
			Assert.That(txNew, Is.Not.Null);
			Assert.That(_WalletManager.Spend(txNew), Is.True);

			// mine the tx from mempool
			_BlockChain.HandleNewBlock(_GenesisBlock.Child().AddTx(TransactionValidation.unpoint(_BlockChain.TxMempool.GetAll()[0])));

			_WalletManager.Import(key);

			txNew = _WalletManager.Sign(key1.Address, Tests.zhash, spendAmount);
			Assert.That(txNew, Is.Null);
		}

		[Test()]
		public void ShouldNotOverspendUsingConfirmedTxXXXXXXXXXX()
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
			_BlockChain.HandleNewBlock(_GenesisBlock);

			Thread.Sleep(1000);


			//_WalletManager.Import();
			var txNew = _WalletManager.Sign(key1.Address, Tests.zhash, spendAmount);
			Assert.That(_WalletManager.Spend(txNew), Is.True);

			var mempoolPtx = _BlockChain.TxMempool.GetAll()[0];
			var _tx = TransactionValidation.unpoint(mempoolPtx);
			_BlockChain.HandleNewBlock(_GenesisBlock.Child().AddTx(_tx));
			Thread.Sleep(1000);
			_WalletManager.Import(key);

			txNew = _WalletManager.Sign(key2.Address, Tests.zhash, spendAmount);
			Assert.That(_WalletManager.Spend(txNew), Is.Null);
		//	Thread.Sleep(1000);

		}
	}
}
