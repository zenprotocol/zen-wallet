using NUnit.Framework;
using System;
using System.Linq;
using Infrastructure;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using Consensus;
using BlockChain;

namespace Zen
{
	[TestFixture()]
	public class UITests
	{
		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

			if (Directory.Exists(App.DefaultBlockChainDB))
			{
				Directory.Delete(App.DefaultBlockChainDB, true);
			}

			if (Directory.Exists(App.DefaultWalletDB))
			{
				Directory.Delete(App.DefaultWalletDB, true);
			}
		}

		[Test(), Order(1)]
		public void CanAquireGenesisOutputsAfterGensis()
		{
			App app = new App();
		//	app.Settings.EndpointOptions.EndpointOption = NBitcoinDerive.EndpointOptions.EndpointOptionsEnum.NoNetworking;

			app.Init();

			app.AddGenesisBlock();

			ulong expectedAmount = 0;

			JsonLoader<Outputs>.Instance.Value.Values.ForEach(o =>
			{
				expectedAmount += o.Amount;
				app.ImportKey(o.Key);
			});

			Assert.That(app.AssetMount(), Is.EqualTo(expectedAmount));

			app.Start();

			new Thread(() =>
			{
				Thread.Sleep(2000);
				app.CloseGUI();
				app.Stop();
			}).Start();

			app.GUI();
		}


		[Test(), Order(1)]
		public void CanAquireGenesisOutputsBeforeGenesis()
		{
			App app = new App();
		//	app.Settings.EndpointOptions.EndpointOption = NBitcoinDerive.EndpointOptions.EndpointOptionsEnum.NoNetworking;

			app.Init();

			ulong expectedAmount = 0;

			JsonLoader<Outputs>.Instance.Value.Values.ForEach(o =>
			{
				expectedAmount += o.Amount;
				app.ImportKey(o.Key);
			});

			Thread.Sleep(000);
			app.AddGenesisBlock();
			Thread.Sleep(1000);

			Assert.That(app.AssetMount(), Is.EqualTo(expectedAmount));
			      
			app.Start();

			new Thread(() =>
			{
				Thread.Sleep(2000);
				app.CloseGUI();
				app.Stop();
			}).Start();

			app.GUI();
		}

		[Test(), Order(2)]
		public async Task CanSendAmounts()
		{
			App app = new App();
		//	app.Settings.EndpointOptions.EndpointOption = NBitcoinDerive.EndpointOptions.EndpointOptionsEnum.NoNetworking;

			app.Init();

			app.AddGenesisBlock();

			JsonLoader<Outputs>.Instance.Value.Values.ForEach(o => app.ImportKey(o.Key));

			app.Start();

			Task.Run(() =>
			{
				Thread.Sleep(1000);
				Assert.That(app.Spend(2), Is.True);
				Thread.Sleep(1000);
				Assert.That(app.Spend(3), Is.True);
				Thread.Sleep(1000);
				Assert.That(app.Spend(4), Is.True);
				Thread.Sleep(1000);
				Assert.That(app.Spend(500), Is.False);
			});

			Task.Run(() =>
			{
				Thread.Sleep(6000);
				app.CloseGUI();
				app.Stop();
			});

			app.GUI();
		//	Thread.Sleep(6000);
		}

		[Test(), Order(2)]
		public async Task ShouldInvalidate()
		{
			App app = new App();
		//	app.Settings.EndpointOptions.EndpointOption = NBitcoinDerive.EndpointOptions.EndpointOptionsEnum.NoNetworking;

			//	app.Settings.EndpointOptions.EndpointOption = NBitcoinDerive.EndpointOptions.EndpointOptionsEnum.NoNetworking;
			app.Init();
			app.AddGenesisBlock();
			
	//		var _NewTx = Infrastructure.Testing.Utils.GetTx().AddOutput(app.GetUnusedKey().Address, Tests.zhash, 1);


			JsonLoader<Outputs>.Instance.Value.Values.ForEach(o => app.ImportKey(o.Key));

			app.Start();

			Task.Run(() =>
			{
				Types.Transaction tx;
				Thread.Sleep(1000);
				Assert.That(app.Spend(2, out tx), Is.True);
				Thread.Sleep(1000);

				var block = app.GenesisBlock.Value.Child().AddTx(tx);
				app.AddBlock(block);

				block = app.GenesisBlock.Value.Child();
				app.AddBlock(block);

				block = block.Child();
				Thread.Sleep(1000);

				app.AddBlock(block);
				Thread.Sleep(1000);
			});

			Task.Run(() =>
			{
				Thread.Sleep(5000);
				app.CloseGUI();
			});

			app.GUI();
			Thread.Sleep(1000); // sleep on main

			Task.Run(() =>
			{
				Thread.Sleep(5000);
				app.CloseGUI();
				app.Stop();
			});

			app.GUI();
		}

		[Test(), Order(2)]
		public async Task ShouldUndoInvalidation()
		{
			App app = new App();
		//	app.Settings.EndpointOptions.EndpointOption = NBitcoinDerive.EndpointOptions.EndpointOptionsEnum.NoNetworking;

			app.Init();
			app.AddGenesisBlock();

			JsonLoader<Outputs>.Instance.Value.Values.ForEach(o => app.ImportKey(o.Key));

			app.Start();

			Task.Run(() =>
			{
				Types.Transaction tx;
				Thread.Sleep(1000);
				Assert.That(app.Spend(2, out tx), Is.True);
				Thread.Sleep(1000);

				var block = app.GenesisBlock.Value.Child().AddTx(tx);
				app.AddBlock(block);

				block = app.GenesisBlock.Value.Child();
				var block1 = block.Child().AddTx(tx);

				app.AddBlock(block1);
				Thread.Sleep(1000);

				app.AddBlock(block);
				//Assert.That(app.AddBlock(block), Is.False);
				Thread.Sleep(1000);
			});

			Task.Run(() =>
			{
				Thread.Sleep(5000);
				app.CloseGUI();
			});

			app.GUI();
			Thread.Sleep(1000); // sleep on main

			Task.Run(() =>
			{
				Thread.Sleep(5000);
				app.CloseGUI();
				app.Stop();
			});

			app.GUI();
		}
	}
}
