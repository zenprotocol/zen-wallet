using NUnit.Framework;
using System;
using NBitcoin;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Consensus;
using Microsoft.FSharp.Collections;

namespace NBitcoinDerive.Tests
{
	[TestFixture()]
	public class NetworkTests : NetworkTestBase
	{
		[Test()]
		public void CanBroadcastTransaction()
		{
			var p = new TestTransactionPool();

			p.Add("test1", 1);
			p.Render();

			AutoResetEvent waitForTransaction = new AutoResetEvent(false);
			bool gotTransaction = false;

			BroadcastTransaction(
				hub =>
				{
					Trace.Information("-- Sending transaction from node group");
					hub.BroadcastTransactionAsync(p["test1"].Value);
				//	Thread.Sleep(1500);
				//	hub.BroadcastTransactionAsync(p["test2"].Value);
					Assert.True(waitForTransaction.WaitOne(15000));
					Assert.True(gotTransaction);
				}, () =>
				{
					gotTransaction = true;
					waitForTransaction.Set();
				}
			);
		}

		[Test()]
		public void ShouldRejectBroadcastedTransaction()
		{
			var p = new TestTransactionPool();

			p.Add("test1", 1);
			p.Add("test2", 0);
			p.Spend("test2", "test1", 1);

			p.Render();

			//ShouldRejectNewTx_DueToReferencedOutputDoesNotExist_DueToMissingOutputIndex

			AutoResetEvent waitForTransaction = new AutoResetEvent(false);
			bool gotTransaction = false;

			BroadcastTransaction(
				hub =>
				{
					Trace.Information("-- Sending transaction from node group");
					hub.BroadcastTransactionAsync(p["test1"].Value);
					Thread.Sleep(1500); //TODO: use reset events instead of sleep?
					hub.BroadcastTransactionAsync(p["test2"].Value);

					Assert.True(waitForTransaction.WaitOne(15000));
					Assert.True(gotTransaction);
				}, () =>
				{
					gotTransaction = true;
					waitForTransaction.Set();
				}
			);
		}

		private void BroadcastTransaction(Action<BroadcastHub> actionSender, Action actionReceiver)
		{
			WithServerSet(2, servers =>
			{
				WithBlockChains(2, blockChains =>
				{
					AutoResetEvent waitForConnection = new AutoResetEvent(false);
					bool connected = false;

					servers.SeedServerIndex = 0;

					AddressManager serverAddressManager = new AddressManager();
					serverAddressManager.Add(
						new NetworkAddress(servers[0].ExternalEndpoint),
						servers[0].ExternalEndpoint.Address
					);
					serverAddressManager.Connected(new NetworkAddress(servers[0].ExternalEndpoint));

					NodeConnectionParameters serverParameters = new NodeConnectionParameters();
					serverParameters.TemplateBehaviors.Add(new AddressManagerBehavior(serverAddressManager));
					serverParameters.TemplateBehaviors.Add(new BroadcastHubBehavior());
					serverParameters.TemplateBehaviors.Add(new SPVBehavior(blockChains[0]));

					blockChains[0].OnAddedToMempool += transaction => {
						Trace.Information("-- Transaction Received (node server)");
						actionReceiver();
					};

					servers[0].InboundNodeConnectionParameters = serverParameters;

					#region NodeGroup

					AddressManager addressManager = new AddressManager();
					addressManager.PeersToFind = 1;
					NodeConnectionParameters nodesGroupParameters = new NodeConnectionParameters();
					nodesGroupParameters.AddressFrom = servers[1].ExternalEndpoint;
					nodesGroupParameters.TemplateBehaviors.Add(new AddressManagerBehavior(addressManager));
					nodesGroupParameters.TemplateBehaviors.Add(new BroadcastHubBehavior());
					nodesGroupParameters.TemplateBehaviors.Add(new SPVBehavior(blockChains[1]));

					blockChains[1].OnAddedToMempool += transaction =>
					{
						Trace.Information("-- Transaction Received (node group)");
					};

					NodesGroup nodesGroup = new NodesGroup(servers.Network, nodesGroupParameters);
					nodesGroup.AllowSameGroup = true;
					nodesGroup.MaximumNodeConnection = 1;
					nodesGroup.ConnectedNodes.Added += (object sender, NodeEventArgs e) =>
					{
						Trace.Information("-- Node added to node group");
						connected = true;
						waitForConnection.Set();
					};
					nodesGroup.Connect();

					#endregion

					Assert.True(waitForConnection.WaitOne(10000)); //TODO: use reset events instead of sleep
					Assert.True(connected);

					actionSender(BroadcastHub.GetBroadcastHub(nodesGroup.NodeConnectionParameters));

					Trace.Information("-- Done");
				});
			});
		}

		[Test()]
		public void CanHandshakeWithAdvertize()
		{
			WithServerSet(2, servers =>
			{
				//var node0address = Handshake(servers[0], servers[1]).MyVersion.AddressFrom;
				//Assert.AreEqual(true, AddressManagerContains(servers[1], node0address));

				Node node0to1 = Handshake(servers[0], servers[1]);
				Thread.Sleep(200); //TODO: use reset events instead of sleep

				Assert.AreEqual(true, AddressManagerContains(servers[1], node0to1.MyVersion.AddressFrom));

				Thread.Sleep(200); //TODO: use reset events instead of sleep
			});
		}

		[Test()]
		public void CanHandshakeSimple()
		{
			WithServerSet(1, servers =>
			{
				NodeConnectionParameters nodeConnectionParameters = new NodeConnectionParameters();

				Node node = Node.Connect(servers[0].Network, servers[0].ExternalEndpoint);

				Assert.AreEqual(NodeState.Connected, node.State);

				node.VersionHandshake();

				Thread.Sleep(200); //TODO: use reset events instead of sleep
			});
		}



		[Test()]
		public void CanConnect()
		{
			WithServerSet(2, servers =>
			{
				Node node0to1 = Connect(servers[0], servers[1]);
				Thread.Sleep(200); //TODO: use reset events instead of sleep
			});
		}

		[Test()]
		public void CanDiscoverPeers()
		{
			int SERVER_COUNT = 4;

			int FIRST_SERVER = 0;
			int SECOND_SERVER = 1;
			int LAST_SERVER = 2;
			int NEW_SERVER = 3;

			var ToBeConnected = new List<NodeServer>();
			var ToBeDiscovered = new List<Node>();

			WithServerSet(SERVER_COUNT, servers =>
			{
				servers.SeedServerIndex = LAST_SERVER; //TODO

				ToBeConnected.Add(servers[FIRST_SERVER]);
				ToBeConnected.Add(servers[SECOND_SERVER]);
				ToBeConnected.Add(servers[LAST_SERVER]);

				ToBeDiscovered.Add(Handshake(servers[FIRST_SERVER], servers[SECOND_SERVER]));
				Trace.Information("Handshake First -> Second");

				ToBeDiscovered.Add(Handshake(servers[SECOND_SERVER], servers[LAST_SERVER]));
				Trace.Information("Handshake Second -> Last");

				#region Setup Parameters for NodeGroup

				AddressManager addressManager = new AddressManager();
				addressManager.PeersToFind = ToBeConnected.Count * 2; // nodes answer to GetAddr with random nodes, so keep trying until we get all expected items

				NodeConnectionParameters parameters = new NodeConnectionParameters();
				parameters.TemplateBehaviors.Add(new AddressManagerBehavior(addressManager));

				parameters.AddressFrom = servers[NEW_SERVER].ExternalEndpoint; //TODO

				NodesGroup nodesGroup = new NodesGroup(servers.Network, parameters);
				nodesGroup.AllowSameGroup = true; //TODO
				nodesGroup.MaximumNodeConnection = ToBeConnected.Count; //TODO

				#endregion

				nodesGroup.Connect();

				AutoResetEvent waitForDiscoveredAll = new AutoResetEvent(false);
				var discoveredAll = false;

				nodesGroup.ConnectedNodes.Added += (object sender, NodeEventArgs e) =>
				{
					Console.WriteLine($"\n\n\nPeer found: {e.Node.Peer.Endpoint}\n\n\n");	

					Node node = ToBeDiscovered.Find(n => n.MyVersion.AddressFrom.Equals(e.Node.Peer.Endpoint));

					if (node != null && ToBeDiscovered.Contains(node))
					{
						ToBeDiscovered.Remove(node);

						if (ToBeDiscovered.Count == 0)
						{
							discoveredAll = true;
							waitForDiscoveredAll.Set();
						}
					}
				};

				Assert.True(waitForDiscoveredAll.WaitOne(30000));
				Assert.True(discoveredAll);
			});
		}
	}
}
