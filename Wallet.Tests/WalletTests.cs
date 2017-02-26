using NUnit.Framework;
using Consensus;
using System.Threading;
using BlockChain;
using System;
using System.Linq;
using BlockChain.Data;
using Wallet.core;

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
		var tx = Utils.GetTx().AddOutput(key.Address, Consensus.Tests.zhash, amount);
		_GenesisBlock = _GenesisBlock.AddTx(tx);

		// pls stop syncing now, wallet
		_WalletManager.Dispose();

		// init the blockchain while wallet's out
		_BlockChain.HandleBlock(_GenesisBlock);

		// new unsynced wallet
		_WalletManager = new WalletManager(_BlockChain, WALLET_DB);

		Action<ResetEventArgs> onReset = a =>
		{
			Assert.That(a.TxDeltaList.Count, Is.EqualTo(1));
			var txBalances = a.TxDeltaList[0];
			Assert.That(txBalances.Transaction, Is.EqualTo(tx));
			Assert.That(txBalances.AssetDeltas[Consensus.Tests.zhash], Is.EqualTo((long)amount));
			walletMessageEvent.Set();
		};
		_WalletManager.OnReset += onReset;

		// sync
		_WalletManager.Import(key);

		Assert.That(walletMessageEvent.WaitOne(3000), Is.True);
		_WalletManager.OnReset -= onReset;
	}

	[Test(), Order(2)]
	public void ShouldRestart()
	{
		_WalletManager.Dispose();

		_WalletManager = new WalletManager(_BlockChain, WALLET_DB);

		Assert.That(_WalletManager.TxDeltaList[0].AssetDeltas[Consensus.Tests.zhash] == 10, Is.True);
	}

	[Test(), Order(3)]
	public void ShouldSeeUnconfirmedTx()
	{
		ulong amount = 11;
		var walletMessageEvent = new AutoResetEvent(false);

		_NewTx = Utils.GetTx().AddOutput(_WalletManager.GetUnusedKey().Address, Consensus.Tests.zhash, amount);

		Action<TxDeltaItemsEventArgs> onItems = a =>
		{
			Assert.That(a.Count, Is.EqualTo(1));
			var txBalances = a[0];
			Assert.That(txBalances.Transaction, Is.EqualTo(_NewTx));
			Assert.That(txBalances.AssetDeltas[Consensus.Tests.zhash], Is.EqualTo((long)amount));
			Assert.That(txBalances.TxState, Is.EqualTo(TxStateEnum.Unconfirmed));
			walletMessageEvent.Set();
		};

		_WalletManager.OnItems += onItems;
		  
		Assert.That(_BlockChain.HandleTransaction(_NewTx), Is.EqualTo(BlockChain.BlockChain.TxResultEnum.Accepted));
		Assert.That(walletMessageEvent.WaitOne(3000), Is.True);
		_WalletManager.OnItems -= onItems;
	}

	[Test(), Order(4)]
	public void ShouldSeeConfirmedTx()
	{
		var walletMessageEvent = new AutoResetEvent(false);

		Action<TxDeltaItemsEventArgs> onItems = a =>
		{
			Assert.That(a.Count, Is.EqualTo(1));
			var txBalances = a[0];
			Assert.That(txBalances.Transaction, Is.EqualTo(_NewTx));
			//Assert.That(txBalances.Balances[Tests.zhash], Is.EqualTo((long)amount));
			Assert.That(txBalances.TxState, Is.EqualTo(TxStateEnum.Confirmed));

			walletMessageEvent.Set();
		};

		_WalletManager.OnItems += onItems;
			//	does a new row get inserted into wallets txs table here!?!?!?!?!?!?!?!

		_BlockChain.HandleBlock(_GenesisBlock.Child().AddTx(_NewTx));
		Assert.That(walletMessageEvent.WaitOne(3000), Is.True);

		_WalletManager.OnItems -= onItems;
	}

	[Test(), Order(5)]
	public void ShouldNotSeeInvalidatedTx()
	{
		//var block = _BlockChain.Tip.Value.Child().AddTx(_NewTx);
		//TODO: should the block be rejected?
		//Assert.That(_BlockChain.HandleBlock(block), Is.EqualTo(AddBk.Result.Rejected));

		var walletMessageEvent = new AutoResetEvent(false);

		Action<TxDeltaItemsEventArgs> onItems = a =>
		{
			if (a.Count(t => t.TxState == TxStateEnum.Unconfirmed) > 0)
				walletMessageEvent.Set();
		};

		_WalletManager.OnItems += onItems;

		var block = _GenesisBlock.Child();

		Assert.That(_BlockChain.HandleBlock(block.Child().AddTx(_NewTx)), Is.True); //TODO: assert: orphan
		Assert.That(_BlockChain.HandleBlock(block), Is.True);

		Assert.That(walletMessageEvent.WaitOne(3000), Is.False);
		_WalletManager.OnItems -= onItems;
	}

	[Test(), Order(6)]
	public void ShouldSeeInvalidatedTx()
	{
		//var block = _BlockChain.Tip.Value.Child().AddTx(_NewTx);
		//TODO: should the block be rejected?
		//Assert.That(_BlockChain.HandleBlock(block), Is.EqualTo(AddBk.Result.Rejected));

		var walletMessageEvent = new AutoResetEvent(false);

		Action<TxDeltaItemsEventArgs> onItems = a =>
		{
			Assert.That(a.Count, Is.EqualTo(1));
			var txBalances = a[0];
			Assert.That(txBalances.Transaction, Is.EqualTo(_NewTx));
			//Assert.That(txBalances.Balances[Tests.zhash], Is.EqualTo((long)amount));
			Assert.That(txBalances.TxState, Is.EqualTo(TxStateEnum.Invalid));

			walletMessageEvent.Set();
		};

		_WalletManager.OnItems += onItems;

		var block = _GenesisBlock.Child();
		Assert.That(_BlockChain.HandleBlock(block), Is.True);
		Assert.That(_BlockChain.HandleBlock(block.Child()), Is.True);

		Assert.That(walletMessageEvent.WaitOne(3000), Is.True);
		_WalletManager.OnItems -= onItems;
	}


	//		TODO: test: are keys marked as used during spend?
}
