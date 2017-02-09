using System;
using Infrastructure;
using System.Threading.Tasks;
using NBitcoinDerive;
using NBitcoin.Protocol;
using System.Collections.Generic;
using NBitcoin;
using NBitcoin.Protocol.Behaviors;
using System.Net;

namespace NBitcoinDerive
{
	public class EndpointOptions {
		public enum EndpointOptionsEnum {
			#if DEBUG
			LocalhostClient,
			LocalhostServer,
			NoNetworking,
			#endif
		}

		public EndpointOptionsEnum EndpointOption { get; set; }
		public IPAddress SpecifiedAddress { get; set; }
	}

	//public interface INodeManager {
	//	void SendTransaction (byte[] address, UInt64 amount);
	//}

	public class NodeManager : ResourceOwner//, INodeManager
	{
		private Server _Server = null;
		private BlockChain.BlockChain _BlockChain = null;
		private Network _Network;
		private NodeConnectionParameters _NodeConnectionParameters;

		#if DEBUG
		public 
		#else
		private
		#endif 
		NATManager _NATManager;

		#if DEBUG
		public 
		#else
		private
		#endif
		NodesGroup _NodesGroup;

		public NodeManager(BlockChain.BlockChain blockChain)
		{
			_BlockChain = blockChain;
			//OwnResource (_BlockChain);
			_Network = JsonLoader<NBitcoinDerive.Network>.Instance.Value;

			AddressManager addressManager = new AddressManager();

			_NodeConnectionParameters = new NodeConnectionParameters();
			var addressManagerBehavior = new AddressManagerBehavior (addressManager);
			_NodeConnectionParameters.TemplateBehaviors.Add(addressManagerBehavior);

			_NATManager = new NATManager(_Network.DefaultPort);
		}

		public void Connect(EndpointOptions endpointOptions)
		{
			IPAddress ipAddress = null;

			if (endpointOptions != null)
			{
				if (endpointOptions.SpecifiedAddress != null)
				{
					ipAddress = endpointOptions.SpecifiedAddress;
				}
#if DEBUG
				if (endpointOptions.EndpointOption == EndpointOptions.EndpointOptionsEnum.NoNetworking)
				{
				}
				else if (endpointOptions.EndpointOption == EndpointOptions.EndpointOptionsEnum.LocalhostServer)
				{
					_Network.DefaultPort = 9999;
					_Network.PeersToFind = 0;
					_Network.MaximumNodeConnection = 0;
					Connect(_NATManager.InternalIPAddress);
				}
				else if (endpointOptions.EndpointOption == EndpointOptions.EndpointOptionsEnum.LocalhostClient)
				{
					//TODO: 
					if (_NATManager.InternalIPAddress != null)
					{
						_Network.DefaultPort = 9999;
						_Network.Seeds.Add(_NATManager.InternalIPAddress.ToString());
						_Network.PeersToFind = 1;
						_Network.MaximumNodeConnection = 1;
					}
					Connect(_NATManager.InternalIPAddress);
				}
#endif
			}
			else
			{
				_NATManager.Init().ContinueWith(t =>
				{
					if (_NATManager.DeviceFound &&
						_NATManager.Mapped.Value &&
						_NATManager.ExternalIPVerified.Value)
					{
						Connect(_NATManager.ExternalIPAddress);
					}
					else
					{
						Connect();
					}
				});
			}
		}

		private void Connect(IPAddress ipAddress = null)
		{
			BroadcastHubBehavior broadcastHubBehavior = new BroadcastHubBehavior(_BlockChain);

			Miner miner = new Miner(_BlockChain);

			_NodeConnectionParameters.TemplateBehaviors.Add(broadcastHubBehavior);
			_NodeConnectionParameters.TemplateBehaviors.Add(new MinerBehavior(miner));
			_NodeConnectionParameters.TemplateBehaviors.Add(new SPVBehavior(_BlockChain, broadcastHubBehavior.BroadcastHub));
			_NodeConnectionParameters.TemplateBehaviors.Add(new ChainBehavior(_BlockChain));

			//_BlockChain.OnAddedToMempool += t => {
			//	broadcastHubBehavior.BroadcastHub.BroadcastTransactionAsync(t);
			//};

			if (ipAddress != null)
			{
				_Server = new Server(ipAddress, _Network, _NodeConnectionParameters);
				OwnResource(_Server);

				if (_Server.Start())
				{
					NodeServerTrace.Information($"Server started at {ipAddress}:{_Network.DefaultPort}");
				}
				else
				{
					NodeServerTrace.Information($"Could not start server at {ipAddress}:{_Network.DefaultPort}");
				}
			}

			if (_Network.Seeds.Count == 0)
			{
				NodeServerTrace.Information("No seeds defined");
				return;
			}

			AddressManagerBehavior.GetAddrman(_NodeConnectionParameters).PeersToFind = _Network.PeersToFind;

			if (_NATManager.ExternalIPAddress != null)
			{
				_NodeConnectionParameters.TemplateBehaviors.Find<AddressManagerBehavior>().Mode = AddressManagerBehaviorMode.AdvertizeDiscover; //parameters.Advertize = true;
				_NodeConnectionParameters.AddressFrom = new System.Net.IPEndPoint(_NATManager.ExternalIPAddress, _Network.DefaultPort);
			}
			else
			{
				_NodeConnectionParameters.TemplateBehaviors.Find<AddressManagerBehavior>().Mode = AddressManagerBehaviorMode.Discover;
			}

			_NodesGroup = new NodesGroup(_Network, _NodeConnectionParameters);
			OwnResource(_NodesGroup);

			_NodesGroup.AllowSameGroup = true; //TODO
			_NodesGroup.MaximumNodeConnection = _Network.MaximumNodeConnection;

			_NodesGroup.ConnectedNodes.Added += (object sender, NodeEventArgs e) =>
			{
				NodeServerTrace.Information("Peer found: " + e.Node.RemoteSocketAddress + ":" + e.Node.RemoteSocketPort);
			};

			_NodesGroup.Connect();
		}
	}
}