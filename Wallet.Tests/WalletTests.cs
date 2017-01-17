using NUnit.Framework;
using Wallet.core;
using System;
using System.Threading;
using System.Collections.Generic;
using Store;
using Consensus;
using Wallet.core.Data;

namespace Wallet.Tests
{
	[TestFixture()]
	public class WalletTests : TestBase
	{
		private List<Output> outputs = null;
		private Keyed<Types.Block> genesisBlock = null;
		 
	    [TestFixtureSetUp]
		public void Init()
		{
			outputs = Utils.CreateOutputsList();
			genesisBlock = Utils.GetGenesisBlock(outputs);
		}

		[TestFixtureTearDown]
		public void Dispose()
		{
			
		}

		[Test()]
		public void ShouldAcquireGenesisOutputAfterBlockAdded()
		{
			TestAction(genesisBlock, (walletManager,blockChain) =>
			{
				blockChain.HandleNewBlock(genesisBlock.Value);
				walletManager.AddKey(outputs[0].Key.PrivateAsString);
				walletManager.Sync();
				Assert.That(Utils.GetBalance(walletManager, outputs[0].Asset), Is.EqualTo(outputs[0].Amount));
			});
		}

		[Test()]
		public void ShouldAcquireGenesisOutputBeforeBlockAdded()
		{
			TestAction(genesisBlock, (walletManager, blockChain) =>
			{
				var addedEvent = new ManualResetEventSlim();

				walletManager.OnNewBalance += t =>
				{
					Assert.That(Utils.GetBalance(walletManager, outputs[0].Asset), Is.EqualTo(outputs[0].Amount));
					addedEvent.Set();
				};

				walletManager.AddKey(outputs[0].Key.PrivateAsString);
				blockChain.HandleNewBlock(genesisBlock.Value);

				addedEvent.Wait();
			});
		}

		[Test()]
		public void ShouldSpend()
		{
			TestAction(genesisBlock, (walletManager, blockChain) =>
			{
				var addedEvent = new ManualResetEventSlim();

				walletManager.OnNewBalance += t =>
				{
					Assert.That(Utils.GetBalance(walletManager, outputs[0].Asset), Is.EqualTo(outputs[0].Amount));
					addedEvent.Set();
				};

				blockChain.HandleNewBlock(genesisBlock.Value);
				walletManager.AddKey(outputs[0].Key.PrivateAsString);
				walletManager.Sync();

				Assert.That(Utils.GetBalance(walletManager, outputs[0].Asset), Is.EqualTo(outputs[0].Amount));


				var result = walletManager.Spend(Key.Create().AddressAsString, outputs[0].Asset, outputs[0].Amount - 2);

				Assert.That(result, Is.EqualTo(true));

				addedEvent.Wait();
			});
		}

	}
}
