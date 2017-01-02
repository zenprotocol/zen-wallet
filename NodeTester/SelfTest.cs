using System;
using NBitcoin.Protocol;
using System.Threading;
using NBitcoin;
using System.Net;
using NBitcoin.Protocol.Behaviors;
using System.Collections.Generic;
using NBitcoinDerive;
using Infrastructure;

namespace NodeTester
{
	public class SelfTest
	{
		LogMessageContext LogMessageContext = new LogMessageContext("Self Test");
	
		public void Start() {
			new Thread (StartCore).Start ();
		}


		private void StartCore() {

			LogMessageContext.Create("Starting...");

			using (var Servers = new TesterNodeServerSet (3)) {

				Node node1 = Handshake (Servers [0], Servers [1], "node1");
				Node node2 = Handshake (Servers [1], Servers [2], "node2");


				NBitcoin.Protocol.AddressManager addressManager = new NBitcoin.Protocol.AddressManager ();

				addressManager.PeersToFind = 4;

				NodeConnectionParameters parameters = new NodeConnectionParameters();
				parameters.TemplateBehaviors.Add(new AddressManagerBehavior(addressManager));

			//	Servers.SeedServerIndex = 2; //TODO

				NodesGroup nodesGroup = new NodesGroup(JsonLoader<Network>.Instance.Value, parameters);
				nodesGroup.AllowSameGroup = true; //TODO
				nodesGroup.MaximumNodeConnection = 2; //TODO


				nodesGroup.Connect ();

				Thread.Sleep (20000);  //TODO

				Console.ForegroundColor = ConsoleColor.Blue;

				LogMessageContext.Create("Found nodes: " + nodesGroup.ConnectedNodes.Count);
					
				foreach (Node node in nodesGroup.ConnectedNodes) {
						LogMessageContext.Create ("Found node: " + node.RemoteSocketAddress + " " + node.RemoteSocketPort);
				}

				LogMessageContext.Create("Stopping");
			}
		}

		public static Node Handshake(NodeServer originServer, NodeServer destServer, String desc)
		{
						
			NodeConnectionParameters nodeConnectionParameters = new NodeConnectionParameters ();

			NBitcoin.Protocol.AddressManager addressManager = new NBitcoin.Protocol.AddressManager ();
			//	Console.WriteLine ("Address Manager of Handshaked Node " + desc + " is " + addressManager.GetHashCode ());

			AddressManagerBehavior addressManagerBehavior = new AddressManagerBehavior (addressManager);
			nodeConnectionParameters.TemplateBehaviors.Add (addressManagerBehavior);
			Node node = Node.Connect (destServer.Network, destServer.ExternalEndpoint, nodeConnectionParameters); 

			node.Advertize = true;
			node.MyVersion.AddressFrom = originServer.ExternalEndpoint;
			node.VersionHandshake();

			return node;
		}
						
		public class TesterNodeServerSet : IDisposable
		{
			LogMessageContext LogMessageContext = new LogMessageContext("Self Test Server");

			private List<NodeServer> nodeServers = new List<NodeServer>();

	//		public TestNetwork Network { get; private set; }

			//public int SeedServerIndex {
			//	set {
			//		Network.AddSeed (new NetworkAddress(nodeServers [value].ExternalEndpoint));
			//	}
			//}

			public TesterNodeServerSet(int count)
			{
		//		Network = new TestNetwork ();

				for (int i = 0; i < count; i++)
				{
					int port = 3380 + i;
					int internalPort = port;

					NodeServer Server = new NodeServer(JsonLoader<Network>.Instance.Value, internalPort: internalPort);

					Server.NodeAdded += (NodeServer sender, Node node) => {
						LogMessageContext.Create("Node added to test server");
					};

					NodeConnectionParameters nodeConnectionParameters = new NodeConnectionParameters ();
					NBitcoin.Protocol.AddressManager addressManager = new NBitcoin.Protocol.AddressManager ();

					nodeConnectionParameters.TemplateBehaviors.Add (new AddressManagerBehavior (addressManager));
					Server.InboundNodeConnectionParameters = nodeConnectionParameters;

					Server.AllowLocalPeers = true;
					Server.ExternalEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1").MapToIPv6Ex(), port);
					Server.Listen();

					nodeServers.Add(Server);
				}
			}

			public NodeServer this[int index]
			{
				get
				{
					return nodeServers[index];
				}
			}

			public void Dispose()
			{
				foreach (NodeServer server in nodeServers)
				{
					server.Dispose();
				}
			}
		}
	}
}