using System;
using Infrastructure;
using System.Threading.Tasks;
using Network;
using NBitcoin.Protocol;
using System.Collections.Generic;
using NBitcoin;
using NBitcoin.Protocol.Behaviors;
using System.Net;

namespace Network
{
	public class NodeManager : ResourceOwner//, INodeManager
	{
		private Server _Server = null;
		private BlockChain.BlockChain _BlockChain = null;
		private NetworkInfo _Network;
		private NodeConnectionParameters _NodeConnectionParameters;

		private Miner _Miner;
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

		bool _MinerEnabled;
		public bool MinerEnabled { set {
				_MinerEnabled = value;

				if (_Miner != null)
					_Miner.Enabled = value;
			}
		}

		public NodeManager(BlockChain.BlockChain blockChain)
		{
			_BlockChain = blockChain;
			//OwnResource (_BlockChain);
			_Network = JsonLoader<NetworkInfo>.Instance.Value;

			AddressManager addressManager = new AddressManager();

			_NodeConnectionParameters = new NodeConnectionParameters();
			var addressManagerBehavior = new AddressManagerBehavior(addressManager);
			_NodeConnectionParameters.TemplateBehaviors.Add(addressManagerBehavior);

			_NATManager = new NATManager(_Network.DefaultPort);
		}

		public void Connect()
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
					Connect(null);
				}
			});
		}

#if DEBUG
		public void ConnectToLocalhost()
		{
			_Network.DefaultPort = 9999;
			_Network.PeersToFind = 1;
			_Network.MaximumNodeConnection = 1;

			if (!_Network.Seeds.Contains(_NATManager.InternalIPAddress.ToString()))
				_Network.Seeds.Add(_NATManager.InternalIPAddress.ToString());	
			
			JsonLoader<NetworkInfo>.Instance.Save();
			Connect(null);
		}

		public void AsLocalhost()
		{
			_Network.DefaultPort = 9999;
			_Network.PeersToFind = 0;
			_Network.MaximumNodeConnection = 0;
			JsonLoader<NetworkInfo>.Instance.Save();
			Connect(_NATManager.InternalIPAddress); 
		}

		public void ConnectToSeed(IPAddress ipAddress)
		{
			_Network.DefaultPort = 9999;
			_Network.PeersToFind = 1;
			_Network.MaximumNodeConnection = 1;

			if (!_Network.Seeds.Contains(ipAddress.ToString()))
				_Network.Seeds.Add(ipAddress.ToString());

			JsonLoader<NetworkInfo>.Instance.Save();
			Connect(null);
		}
#endif

		public void Connect(IPAddress ipAddress)
		{
			BroadcastHubBehavior broadcastHubBehavior = new BroadcastHubBehavior();

			_Miner = new Miner(_BlockChain);
			_Miner.Enabled = _MinerEnabled;

			_NodeConnectionParameters.TemplateBehaviors.Add(broadcastHubBehavior);
			_NodeConnectionParameters.TemplateBehaviors.Add(new MinerBehavior(_Miner));
			_NodeConnectionParameters.TemplateBehaviors.Add(new SPVBehavior(_BlockChain, broadcastHubBehavior.BroadcastHub));
			_NodeConnectionParameters.TemplateBehaviors.Add(new ChainBehavior(_BlockChain));

			AddressManagerBehavior.GetAddrman(_NodeConnectionParameters).PeersToFind = _Network.PeersToFind;

			if (ipAddress != null)
			{
				_NodeConnectionParameters.TemplateBehaviors.Find<AddressManagerBehavior>().Mode = AddressManagerBehaviorMode.AdvertizeDiscover; //parameters.Advertize = true;
				_NodeConnectionParameters.AddressFrom = new System.Net.IPEndPoint(ipAddress, _Network.DefaultPort);
			}
			else
			{
				_NodeConnectionParameters.TemplateBehaviors.Find<AddressManagerBehavior>().Mode = AddressManagerBehaviorMode.Discover;
			}

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
			}
			else
			{
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
}