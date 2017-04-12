using System;
using Infrastructure;
using System.Threading.Tasks;
using Network;
using NBitcoin.Protocol;
using System.Collections.Generic;
using NBitcoin;
using NBitcoin.Protocol.Behaviors;
using System.Net;
using Consensus;

namespace Network
{
	public class NodeManager : ResourceOwner//, INodeManager
	{
		private Server _Server = null;
		private BlockChain.BlockChain _BlockChain = null;
		private NetworkInfo _Network;
		private NodeConnectionParameters _NodeConnectionParameters;
		private BroadcastHubBehavior _BroadcastHubBehavior;

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
		public bool MinerEnabled
		{
			set
			{
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

		public async Task Connect()
		{
			IPAddress ipAddress = null;

#if DEBUG
			if (_Network.IsLANHost)
			{
				ipAddress = _NATManager.InternalIPAddress;
				_Network.PeersToFind = 0;
			}
			else if (_Network.IsLANClient)
			{
				ipAddress = null;
				_Network.PeersToFind = 1;
				_Network.Seeds.Clear();
				_Network.Seeds.Add(_NATManager.InternalIPAddress.ToString());
			}
			else
#endif
			if (!string.IsNullOrEmpty(_Network.ExternalIPAddress))
			{
				ipAddress = _NATManager.ExternalIPAddress;
			}
			else
			{
				await _NATManager.Init();

				if (_NATManager.DeviceFound &&
					_NATManager.Mapped.Value &&
					_NATManager.ExternalIPVerified.Value)
				{
					ipAddress = _NATManager.ExternalIPAddress;
				}
			}

			_BroadcastHubBehavior = new BroadcastHubBehavior();

			_Miner = new Miner(_BlockChain);
			_Miner.Enabled = _MinerEnabled;

			_NodeConnectionParameters.TemplateBehaviors.Add(_BroadcastHubBehavior);
			_NodeConnectionParameters.TemplateBehaviors.Add(new MinerBehavior(_Miner));
			_NodeConnectionParameters.TemplateBehaviors.Add(new SPVBehavior(_BlockChain, _BroadcastHubBehavior.BroadcastHub));
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

		/// <summary>
		/// Transmits a tx on the network.
		/// </summary>
		public BlockChain.BlockChain.TxResultEnum Transmit(Types.Transaction tx)
		{
			var result = _BlockChain.HandleTransaction(tx);

			if (result == BlockChain.BlockChain.TxResultEnum.Accepted && _BroadcastHubBehavior != null)
				_BroadcastHubBehavior.BroadcastHub.BroadcastTransactionAsync(tx);

			return result;
		}
	}
}