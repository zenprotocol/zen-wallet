using NUnit.Framework;
using Consensus;
using System.Threading;
using Infrastructure.Testing;
using BlockChain;

namespace Wallet.core
{
	[TestFixture()]
	public class WalletTests : WalletTestsBase
	{
		[Test(), Order(1)]
		public void ShouldImport()
		{
			ulong amount = 10;
			var walletMessageEvent = new AutoResetEvent(false);

			// init genesis with a key from wallet
			var key = _WalletManager.GetUnusedKey();
			var tx = Utils.GetTx().AddOutput(key.Address, Tests.zhash, amount);
			_GenesisBlock = _GenesisBlock.AddTx(tx);

			// pls stop syncing now, wallet
			_WalletManager.Dispose();

			// init the blockchain while wallet's out
			_BlockChain.HandleNewBlock(_GenesisBlock);

			// new unsynced wallet
			_WalletManager = new WalletManager(_BlockChain, WALLET_DB);

			_WalletManager.OnReset += a =>
			{
				Assert.That(a.TxDeltaList.Count, Is.EqualTo(1));
				var txBalances = a.TxDeltaList[0];
				Assert.That(txBalances.Transaction, Is.EqualTo(tx));
				Assert.That(txBalances.AssetDeltas[Tests.zhash], Is.EqualTo((long)amount));
				walletMessageEvent.Set();
			};

			// sync
			_WalletManager.Import(key);

			Assert.That(walletMessageEvent.WaitOne(3000), Is.True);
		}

		[Test(), Order(2)]
		public void ShouldRestart()
		{
			_WalletManager.Dispose();

			_WalletManager = new WalletManager(_BlockChain, WALLET_DB);

			Assert.That(_WalletManager.TxDeltaList[0].AssetDeltas[Tests.zhash] == 10, Is.True);
		}

		[Test(), Order(3)]
		public void ShouldSeeUnconfirmedTx()
		{
			ulong amount = 11;
			var walletMessageEvent = new AutoResetEvent(false);

			_NewTx = Utils.GetTx().AddOutput(_WalletManager.GetUnusedKey().Address, Tests.zhash, amount);

			_WalletManager.OnItems += a =>
			{
				Assert.That(a.Count, Is.EqualTo(1));
				var txBalances = a[0];
				Assert.That(txBalances.Transaction, Is.EqualTo(_NewTx));
				Assert.That(txBalances.AssetDeltas[Tests.zhash], Is.EqualTo((long)amount));
				Assert.That(txBalances.TxState, Is.EqualTo(TxStateEnum.Unconfirmed));
				walletMessageEvent.Set();
			};
	
			Assert.That(_BlockChain.HandleNewTransaction(_NewTx), Is.EqualTo(AddTx.Result.Added));
			Assert.That(walletMessageEvent.WaitOne(3000), Is.True);
		}

		[Test(), Order(4)]
		public void ShouldSeeConfirmedTx()
		{
			var walletMessageEvent = new AutoResetEvent(false);

			var newBlock = _GenesisBlock.Child().AddTx(_NewTx);

			_WalletManager.OnItems += a =>
			{
				Assert.That(a.Count, Is.EqualTo(1));
				var txBalances = a[0];
				Assert.That(txBalances.Transaction, Is.EqualTo(_NewTx));
				//Assert.That(txBalances.Balances[Tests.zhash], Is.EqualTo((long)amount));
				Assert.That(txBalances.TxState, Is.EqualTo(TxStateEnum.Confirmed));

				walletMessageEvent.Set();
			};

				//	does a new row gets inserted into wallets txs table here!?!?!?!?!?!?!?!
			_BlockChain.HandleNewBlock(newBlock);
			Assert.That(walletMessageEvent.WaitOne(3000), Is.True);
		}


		//		TODO: test: are keys marked as used during spend?
	}
}
