using System;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using System.Net;
using NBitcoin.Protocol.Filters;
using Infrastructure;
using NodeCore;

namespace NodeCore
{
	public class Server
	{
		private readonly NodeServer _Server;

		public NodeBehaviorsCollection Behaviors
		{
			get
			{
				return _Server.InboundNodeConnectionParameters.TemplateBehaviors;
			}
		}

		public Server(IResourceOwner resourceOwner, IPEndPoint externalEndpoint, NBitcoin.Network network)
		{
			_Server = new NodeServer(network, internalPort: externalEndpoint.Port);
			resourceOwner.OwnResource(_Server);

			NodeConnectionParameters nodeConnectionParameters = new NodeConnectionParameters();
			NBitcoin.Protocol.AddressManager addressManager = AddressManager.Instance.GetBitcoinAddressManager(); // new NBitcoin.Protocol.AddressManager ();

			var addressManagerBehavior = new AddressManagerBehavior (addressManager);
		//	addressManagerBehavior.Mode = hasExternal ? AddressManagerBehaviorMode.AdvertizeDiscover : AddressManagerBehaviorMode.Discover;
			nodeConnectionParameters.TemplateBehaviors.Add(addressManagerBehavior);

			_Server.InboundNodeConnectionParameters = nodeConnectionParameters;
			_Server.AllowLocalPeers = true; //TODO
			_Server.ExternalEndpoint = externalEndpoint;

			Trace.Information($"Server setup at {externalEndpoint}");
		}

		public bool Start()
		{
			try
			{
				_Server.Listen();
				Trace.Information("Server listening");
				return true;
			}
			catch (Exception e)
			{
				Trace.Error("Listen", e);
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

