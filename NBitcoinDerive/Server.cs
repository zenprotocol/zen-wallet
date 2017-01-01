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

		public Server(IPEndPoint externalEndpoint, Network network, AddressManager addressManager)
		{
			_Server = new NodeServer(network, internalPort: externalEndpoint.Port);
			OwnResource(_Server);

			NodeConnectionParameters nodeConnectionParameters = new NodeConnectionParameters();

			var addressManagerBehavior = new AddressManagerBehavior (addressManager);
		//	addressManagerBehavior.Mode = hasExternal ? AddressManagerBehaviorMode.AdvertizeDiscover : AddressManagerBehaviorMode.Discover;
			nodeConnectionParameters.TemplateBehaviors.Add(addressManagerBehavior);

			_Server.InboundNodeConnectionParameters = nodeConnectionParameters;
			_Server.AllowLocalPeers = true; //TODO
			_Server.ExternalEndpoint = externalEndpoint;

			NodeServerTrace.Information($"Server setup at {externalEndpoint}");
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

