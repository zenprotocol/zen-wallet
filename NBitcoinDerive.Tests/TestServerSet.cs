using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using NUnit.Framework;

namespace NBitcoinDerive.Tests
{
	public class TestServerSet : IDisposable
	{
		private List<NodeServer> nodeServers = new List<NodeServer>();

		public TestNetwork Network { get; private set; }

		public int SeedServerIndex
		{
			set
			{
				Network.AddSeed(new NetworkAddress(nodeServers[value].ExternalEndpoint));
			}
		}

		public TestServerSet(int count)
		{
			Network = new TestNetwork();

			for (int i = 0; i < count; i++)
			{
				int port = 3380 + i;
				int internalPort = port;

				NodeServer Server = new NodeServer(Network, internalPort: internalPort);

				NodeConnectionParameters nodeConnectionParameters = new NodeConnectionParameters();
				AddressManager addressManager = new AddressManager();

				nodeConnectionParameters.TemplateBehaviors.Add(new AddressManagerBehavior(addressManager));
				Server.InboundNodeConnectionParameters = nodeConnectionParameters;


				Server.AllowLocalPeers = true;
				Server.ExternalEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1").MapToIPv6Ex(), port);
				Server.Listen();

				Assert.AreEqual(true, Server.IsListening);

				nodeServers.Add(Server);
			}

			Trace.Information("\n\nServers are listening\n\n");
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
