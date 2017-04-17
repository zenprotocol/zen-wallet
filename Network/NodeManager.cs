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
		Server _Server = null;
		BlockChain.BlockChain _BlockChain = null;
		NodeConnectionParameters _NodeConnectionParameters;
		BroadcastHubBehavior _BroadcastHubBehavior;
		Miner _Miner;
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
			AddressManager addressManager = new AddressManager();

			_NodeConnectionParameters = new NodeConnectionParameters();
			var addressManagerBehavior = new AddressManagerBehavior(addressManager);
			_NodeConnectionParameters.TemplateBehaviors.Add(addressManagerBehavior);

		}

		public async Task Connect(NetworkInfo networkInfo)
		{
			IPAddress ipAddress = null;
			var natManager = new NATManager(networkInfo.DefaultPort);

#if DEBUG
			if (networkInfo.IsLANHost)
			{
				ipAddress = natManager.InternalIPAddress;
				networkInfo.PeersToFind = 0;
			}
			else if (networkInfo.IsLANClient)
			{
				ipAddress = null;
				networkInfo.PeersToFind = 1;
				networkInfo.Seeds.Clear();
				networkInfo.Seeds.Add(natManager.InternalIPAddress.ToString());
			}
			else
#endif
			if (!string.IsNullOrEmpty(networkInfo.ExternalIPAddress))
			{
				ipAddress = IPAddress.Parse(networkInfo.ExternalIPAddress);
			}
			else
			{

				await natManager.Init();

				if (natManager.DeviceFound &&
					natManager.Mapped.Value &&
					natManager.ExternalIPVerified.Value)
				{
					ipAddress = natManager.ExternalIPAddress;
				}
			}

			_BroadcastHubBehavior = new BroadcastHubBehavior();

			_Miner = new Miner(_BlockChain);
			_Miner.Enabled = _MinerEnabled;

			_NodeConnectionParameters.TemplateBehaviors.Add(_BroadcastHubBehavior);
			_NodeConnectionParameters.TemplateBehaviors.Add(new MinerBehavior(_Miner));
			_NodeConnectionParameters.TemplateBehaviors.Add(new SPVBehavior(_BlockChain, _BroadcastHubBehavior.BroadcastHub));
			_NodeConnectionParameters.TemplateBehaviors.Add(new ChainBehavior(_BlockChain));

			AddressManagerBehavior.GetAddrman(_NodeConnectionParameters).PeersToFind = networkInfo.PeersToFind;

			if (ipAddress != null)
			{
				_NodeConnectionParameters.TemplateBehaviors.Find<AddressManagerBehavior>().Mode = AddressManagerBehaviorMode.AdvertizeDiscover; //parameters.Advertize = true;
				_NodeConnectionParameters.AddressFrom = new System.Net.IPEndPoint(ipAddress, networkInfo.DefaultPort);
			}
			else
			{
				_NodeConnectionParameters.TemplateBehaviors.Find<AddressManagerBehavior>().Mode = AddressManagerBehaviorMode.Discover;
			}

			if (ipAddress != null)
			{ 
				_Server = new Server(ipAddress, networkInfo, _NodeConnectionParameters);
				OwnResource(_Server);

				if (_Server.Start())
				{
					NodeServerTrace.Information($"Server started at {ipAddress}:{networkInfo.DefaultPort}");
				}
				else
				{
					NodeServerTrace.Information($"Could not start server at {ipAddress}:{networkInfo.DefaultPort}");
				}
			}

			if (networkInfo.Seeds.Count == 0)
			{
				NodeServerTrace.Information("No seeds defined");
			}
			else
			{
				_NodesGroup = new NodesGroup(networkInfo, _NodeConnectionParameters);
				OwnResource(_NodesGroup);

				_NodesGroup.AllowSameGroup = true; //TODO
				_NodesGroup.MaximumNodeConnection = networkInfo.MaximumNodeConnection;

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