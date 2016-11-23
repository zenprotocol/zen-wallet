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
			WithServerSet(2, servers =>
			{
				WithBlockChains(2, blockChains =>
				{
					AutoResetEvent waitForTransaction = new AutoResetEvent(false);
					AutoResetEvent waitForConnection = new AutoResetEvent(false);
					bool connected = false;
					bool gotTransaction = false;

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
						gotTransaction = true;
						waitForTransaction.Set();
					};

					servers[0].InboundNodeConnectionParameters = serverParameters;

					#region Setup Parameters for NodeGroup

					AddressManager addressManager = new AddressManager();
					addressManager.PeersToFind = 1;

					NodeConnectionParameters parameters = new NodeConnectionParameters();
					parameters.TemplateBehaviors.Add(new AddressManagerBehavior(addressManager));

					parameters.TemplateBehaviors.Add(new BroadcastHubBehavior());
					parameters.TemplateBehaviors.Add(new SPVBehavior(blockChains[1]));

					blockChains[1].OnAddedToMempool += transaction =>
					{
						Trace.Information("-- Transaction Received (node group)");
					};

					parameters.AddressFrom = servers[1].ExternalEndpoint;

					NodesGroup nodesGroup = new NodesGroup(servers.Network, parameters);
					nodesGroup.AllowSameGroup = true;
					nodesGroup.MaximumNodeConnection = 1;

					#endregion

					nodesGroup.Connect();

					nodesGroup.ConnectedNodes.Added += (object sender, NodeEventArgs e) =>
					{
						Trace.Information("-- Node added to node group");
						connected = true;
						waitForConnection.Set();
					};

					Assert.True(waitForConnection.WaitOne(10000));
					Assert.True(connected);

					var hub = BroadcastHub.GetBroadcastHub(nodesGroup.NodeConnectionParameters);

					Trace.Information("-- Sending transaction from node group");


					var p = new TestTransactionPool();

					p.Add("base", 1);
					p.Render();


					hub.BroadcastTransactionAsync(p["base"].Value);

					Assert.True(waitForTransaction.WaitOne(10000));
					Assert.True(gotTransaction);

					Trace.Information("-- Done");
				});
			});
		}

		[Test()]
		public void CanHandshake()
		{
			WithServerSet(2, servers =>
			{
				//var node0address = Handshake(servers[0], servers[1]).MyVersion.AddressFrom;
				//Assert.AreEqual(true, AddressManagerContains(servers[1], node0address));

				Node node0to1 = Handshake(servers[0], servers[1]);
				Thread.Sleep(200);

				Assert.AreEqual(true, AddressManagerContains(servers[1], node0to1.MyVersion.AddressFrom));

				Thread.Sleep(200);
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
				addressManager.PeersToFind = ToBeConnected.Count;

				NodeConnectionParameters parameters = new NodeConnectionParameters();
				parameters.TemplateBehaviors.Add(new AddressManagerBehavior(addressManager));

				parameters.AddressFrom = servers[NEW_SERVER].ExternalEndpoint; //TODO

				NodesGroup nodesGroup = new NodesGroup(servers.Network, parameters);
				nodesGroup.AllowSameGroup = true; //TODO
				nodesGroup.MaximumNodeConnection = ToBeConnected.Count; //TODO

				#endregion

				nodesGroup.Connect();

				int connectedNodesCounter = 0;

				nodesGroup.ConnectedNodes.Added += (object sender, NodeEventArgs e) =>
				{
					Console.WriteLine($"\n\n\nPeer found: {e.Node.Peer.Endpoint}\n\n\n");	
					connectedNodesCounter++;
					Node Node = ToBeDiscovered.Find(node => node.MyVersion.AddressFrom.Equals(e.Node.Peer.Endpoint));

					Assert.IsNotNull(Node);
					ToBeDiscovered.Remove(Node);

					//if (ToBeDiscovered.Count == 0 && ToBeConnected.Count == connectedNodesCounter)
					//{
					//	return;
					//}
				};

				Thread.Sleep(19000);  //TODO

				//throw new Exception();

				Assert.IsEmpty(ToBeDiscovered);
				Assert.AreEqual(ToBeConnected.Count, connectedNodesCounter); 
			});
		}
	}
}
