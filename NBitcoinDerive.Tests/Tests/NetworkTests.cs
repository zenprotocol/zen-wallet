using NUnit.Framework;
using System;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace NBitcoinDerive.Tests
{
	[TestFixture()]
	public class NetworkTests : NetworkTestBase
	{
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
