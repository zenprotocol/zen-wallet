using System;
using System.Linq;
using System.Net;
using Infrastructure.Testing.Blockchain;
using NBitcoin.Protocol;
using System.Threading;
using NBitcoin.Protocol.Behaviors;
using System.Collections.Generic;
using DBreeze;
using BlockChain.Store;
using Store;
using System.IO;

namespace Test
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			var p = new Infrastructure.Testing.Blockchain.TestTransactionPool();

			p.Add("t1", 1);
			p.Add("t2", 1);
			p.Add("t3", 0);
			p.Spend("t2", "t1", 0);
			p.Spend("t3", "t2", 0);

			p.Render();

			var genesisBlock = new TestBlock(p.TakeOut("t1").Value);
			genesisBlock.Render ();
			var block1 = new TestBlock(p.TakeOut("t2").Value);
			block1.Parent = genesisBlock;
			block1.Render ();


			WithBlockChains (1, genesisBlock.Value.Key, blockChains => {
				blockChains[0].HandleNewBlock(genesisBlock.Value.Value);
				blockChains[0].HandleNewBlock(block1.Value.Value);

				blockChains[0].HandleNewTransaction(p.TakeOut("t3").Value);

				var x = blockChains[0].MineAllInMempool();

				Console.WriteLine(x);
			});
		}

		public static void ___Main(string[] args)
		{
			var store = new OrphanBlockStore();

			var p1 = new Infrastructure.Testing.Blockchain.TestTransactionPool();
			p1.Add("t1", 0);
			p1.Add("t2", 0);
			p1.Add("t3", 0);
			p1.Add("t4", 0);
			p1.Render();


			var block1 = new TestBlock(p1.TakeOut("t1").Value);
			var block2 = new TestBlock(p1.TakeOut("t2").Value);
			var block3 = new TestBlock(p1.TakeOut("t3").Value);
			var block4 = new TestBlock(p1.TakeOut("t4").Value);


			block2.Parent = block1;
			block3.Parent = block1;
			block4.Parent = block3; // gotcha!

			block1.Render();
			block2.Render();
			block3.Render();
			block4.Render();

			using (TestDBContext dbContext = new TestDBContext())
			{
				using (TransactionContext transactionContext = dbContext.GetTransactionContext())
				{
					store.Put(transactionContext, block2.Value);
					store.Put(transactionContext, block3.Value);
					store.Put(transactionContext, block4.Value);

					var result = store.GetOrphansOf(transactionContext, block1.Value);

				//	CollectionAssert.Contains(result, block2.Value);
				//	CollectionAssert.Contains(result, block3.Value);
				}
			}	
		}

		public static void __Main(string[] args)
		{
			DBreezeEngine engine = null;
			if(engine == null)
				engine = new DBreezeEngine(@"test_xxx");

			using (var tran = engine.GetTransaction())
			{
				tran.Insert<byte[], string> ("xxx", new byte[] { 0x00, 0x01, 0x02, 0x00 }, "value2");
				tran.Insert<byte[], string> ("xxx", new byte[] { 0x00, 0x01, 0x02, 0x01 }, "value3");

				foreach (var row in tran.SelectForwardStartFrom<byte[], string>("xxx", new byte[] { 0x00, 0x01, 0x02 }, true))
				{
					Console.WriteLine("K: {0}; V: {1}", BitConverter.ToString(row.Key), (row.Value == null) ? "NULL" : row.Value.ToString());
				}

//				tran.Commit();
			}
		}

		public static void _Main(string[] args)
		{
			var network = new TestNetwork();

			network.AddSeed(new NetworkAddress(new IPEndPoint(IPAddress.Parse("192.168.2.101"), 9999)));

			var p = new Infrastructure.Testing.Blockchain.TestTransactionPool();

			p.Add("t1", 1);
			p.Add("t2", 0);
			p.Spend("t2", "t1", 0);

			p.Render();

			var genesisBlock = new TestBlock(p.TakeOut("t1").Value);
			var block1 = new TestBlock(p.TakeOut("t2").Value);
			block1.Parent = genesisBlock;

			genesisBlock.Render();
			block1.Render();

			WithBlockChains(1, genesisBlock.Value.Key, blockChains =>
			{
				//	blockChains[0].HandleNewBlock(genesisBlock.Value.Value);
				//	blockChains[0].HandleNewBlock(block1.Value.Value);


				AutoResetEvent waitForConnection = new AutoResetEvent(false);
				bool connected = false;

				blockChains[0].OnAddedToStore += transaction =>
				{
					Console.WriteLine("-- Transaction Received (node server)");
					//	actionReceiver();
				};

				NBitcoin.Protocol.AddressManager addressManager = new AddressManager();
				addressManager.PeersToFind = 1;
				NodeConnectionParameters nodesGroupParameters = new NodeConnectionParameters();
				//				nodesGroupParameters.AddressFrom = servers[1].ExternalEndpoint;
				nodesGroupParameters.TemplateBehaviors.Add(new AddressManagerBehavior(addressManager));
				nodesGroupParameters.TemplateBehaviors.Add(new ChainBehavior(blockChains[0]));
				nodesGroupParameters.TemplateBehaviors.Add(new BroadcastHubBehavior());
				nodesGroupParameters.TemplateBehaviors.Add(new SPVBehavior(blockChains[0], BroadcastHub.GetBroadcastHub(nodesGroupParameters.TemplateBehaviors)));

				NodesGroup nodesGroup = new NodesGroup(network, nodesGroupParameters);
				nodesGroup.AllowSameGroup = true;
				nodesGroup.MaximumNodeConnection = 1;
				nodesGroup.ConnectedNodes.Added += (object sender, NodeEventArgs e) =>
				{
					Console.WriteLine("-- Node added to node group");
					connected = true;
					waitForConnection.Set();
				};
				nodesGroup.Connect();


			//	Assert.True(waitForConnection.WaitOne(10000)); //TODO: use reset events instead of sleep
			//	Assert.True(connected);



				//TODO
				Thread.Sleep(40000);





				//					actionSender(BroadcastHub.GetBroadcastHub(nodesGroup.NodeConnectionParameters));

			//	Trace.Information("-- Done");
			});
		}

		private static void WithBlockChains(int blockChains, byte[] genesisBlockHash, Action<BlockChain.BlockChain[]> action)
		{
			List<TestBlockChain> testBlockChains = new List<TestBlockChain>();

			for (int i = 0; i < blockChains; i++)
			{
				String dbName = "test-" + new Random().Next(0, 1000);
				testBlockChains.Add(new TestBlockChain(dbName, genesisBlockHash));
			}

			action(testBlockChains.Select(t => t.BlockChain).ToArray());

			foreach (var testBlockChain in testBlockChains)
			{
				testBlockChain.Dispose();
			}
		}

		protected class TestDBContext : IDisposable
		{
			public string DbName { get; private set; }
			private readonly DBContext _DBContext;

			public TestDBContext()
			{
				DbName = "test-" + new Random().Next(0, 1000);
				_DBContext = new DBContext(DbName);
			}

			public void Dispose()
			{
				_DBContext.Dispose();
				Directory.Delete(DbName, true);
			}

			public TransactionContext GetTransactionContext()
			{
				return _DBContext.GetTransactionContext();
			}
		}

	}
}
