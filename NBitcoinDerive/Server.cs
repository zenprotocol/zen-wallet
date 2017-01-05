using System;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using System.Net;
using Infrastructure;

namespace NBitcoinDerive
{
	public class Server : ResourceOwner
	{
		private readonly NodeServer _Server;

		public NodeBehaviorsCollection Behaviors
		{
			get
			{
				return _Server.InboundNodeConnectionParameters.TemplateBehaviors;
			}
		}

		public Server(IPAddress externalAddress, Network network, NodeConnectionParameters nodeConnectionParameters)
		{
			_Server = new NodeServer(network, internalPort: network.DefaultPort);
			OwnResource(_Server);

			if (externalAddress != null) 
			{
				var externalEndpoint = new IPEndPoint (externalAddress, network.DefaultPort);
				AddressManager addressManager = AddressManagerBehavior.GetAddrman (nodeConnectionParameters);
				addressManager.Add (new NetworkAddress(externalEndpoint));
				addressManager.Connected (new NetworkAddress(externalEndpoint));
				_Server.ExternalEndpoint = externalEndpoint;
			}

			_Server.InboundNodeConnectionParameters = nodeConnectionParameters;
			_Server.AllowLocalPeers = true; //TODO

			NodeServerTrace.Information($"Server setup at {externalAddress}");
		}

		public bool Start()
		{
			try
			{
				_Server.Listen();
				NodeServerTrace.Information("Server listening");
				return true;
			}
			catch (Exception e)
			{
				NodeServerTrace.Error("Listen", e);
			}
			return false;
		}

		public void Stop()
		{
			_Server.Dispose();
		}

		public bool IsListening
		{
			get
			{
				return _Server.IsListening;
			}
		}

		public void OnNodeAdded(NodeServerNodeEventHandler nodeServerNodeEventHandler)
		{
			_Server.NodeAdded += nodeServerNodeEventHandler;
		}
	}
}

