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
using BlockChain.Data;
using static BlockChain.BlockVerificationHelper;

namespace Network
{
	public class NodeManager : ResourceOwner//, INodeManager
	{
		Server _Server = null;
		BlockChain.BlockChain _BlockChain = null;
		NodeConnectionParameters _NodeConnectionParameters;
		BroadcastHubBehavior _BroadcastHubBehavior;
        MinerBehavior _MinerBehavior;
		NodesGroup _NodesGroup;

		//public readonly Miner Miner;

		public NodeManager(BlockChain.BlockChain blockChain)
		{
			_BlockChain = blockChain;
			var addressManager = new AddressManager();

			_NodeConnectionParameters = new NodeConnectionParameters();
			var addressManagerBehavior = new AddressManagerBehavior(addressManager);
			_NodeConnectionParameters.TemplateBehaviors.Add(addressManagerBehavior);

			//Miner = new Miner();

            //OwnResource(Miner);
			//Miner.BlockChain_ = blockChain;
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

                if (networkInfo.Seeds.Count == 0 && natManager.InternalIPAddress != null && !networkInfo.Seeds.Contains(natManager.InternalIPAddress.ToString()))
                {
                    networkInfo.Seeds.Add(natManager.InternalIPAddress.ToString());
                }
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
            _MinerBehavior = new MinerBehavior();

			_NodeConnectionParameters.TemplateBehaviors.Add(_BroadcastHubBehavior);
            _NodeConnectionParameters.TemplateBehaviors.Add(_MinerBehavior);
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
            //TODO: if tx is known (and validated), just send it.
			var result = new HandleTransactionAction { Tx = tx }.Publish().Result;

			if (result == BlockChain.BlockChain.TxResultEnum.Accepted && _BroadcastHubBehavior != null)
				_BroadcastHubBehavior.BroadcastHub.BroadcastTransactionAsync(tx);

			return result;
		}

  //      public BkResultEnum Transmit(Types.Block bk)
		//{
  //          var result = new HandleBlockAction(bk).Publish().Result.BkResultEnum;

  //          if (result == BkResultEnum.Accepted)
  //              _MinerBehavior.BroadcastBlock(bk);

		//	return result;
		//}

		public void Transmit(Types.Block bk)
		{
            if (_MinerBehavior != null)
	    		_MinerBehavior.BroadcastBlock(bk);
		}
	}
}