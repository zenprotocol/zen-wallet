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
	public class NodeManager : ResourceOwner
	{
		Server _Server = null;
		BlockChain.BlockChain _BlockChain = null;
		NodeConnectionParameters _NodeConnectionParameters;
		BroadcastHubBehavior _BroadcastHubBehavior;
		MinerBehavior _MinerBehavior;
		NodesGroup _NodesGroup;

		public NodeManager(BlockChain.BlockChain blockChain)
		{
			_BlockChain = blockChain;
			var addressManager = new AddressManager();

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
                ipAddress = natManager.InternalIPAddress ?? IPAddress.Loopback;
				networkInfo.PeersToFind = 0;
			}
			else if (networkInfo.IsLANClient)
			{
                var ipAddressStr = (natManager.InternalIPAddress ?? IPAddress.Loopback).ToString();
				networkInfo.PeersToFind = 1;

                if (networkInfo.Seeds.Count == 0 && !networkInfo.Seeds.Contains(ipAddressStr))
				{
					networkInfo.Seeds.Add(ipAddressStr);
				}
			}
			else
#endif
			if (!string.IsNullOrEmpty(networkInfo.ExternalIPAddress))
			{
				ipAddress = IPAddress.Parse(networkInfo.ExternalIPAddress);
			}
			else if (networkInfo.DisableUPnP)
			{
				StatusMessageProducer.OutboundStatus = OutboundStatusEnum.Disabled;
			}
			else
			{
				StatusMessageProducer.OutboundStatus = OutboundStatusEnum.Initializing;
				await natManager.Init();

				if (natManager.DeviceFound &&
					natManager.Mapped.Value &&
					natManager.ExternalIPVerified.HasValue &&
					natManager.ExternalIPVerified.Value)
				{
					StatusMessageProducer.OutboundStatus = OutboundStatusEnum.HasValidAddress;
					ipAddress = natManager.ExternalIPAddress;
				}
				else
				{
					StatusMessageProducer.OutboundStatus = OutboundStatusEnum.HasInvalidAddress;
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
					StatusMessageProducer.OutboundStatus = OutboundStatusEnum.Accepting;
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

#if TRACE
				_NodesGroup.ConnectedNodes.Added += (object sender, NodeEventArgs e) =>
				{
					NodeServerTrace.Information("Peer found: " + e.Node.RemoteSocketAddress + ":" + e.Node.RemoteSocketPort);
				};
#endif

				_NodesGroup.Connect();
			}
		}

		/// <summary>
		/// Transmits a tx on the network.
		/// </summary>
		public Task<BlockChain.BlockChain.TxResultEnum> Transmit(Types.Transaction tx)
		{
            //TODO: refactor
            //TODO: if tx is known (and validated), just send it.
            return new HandleTransactionAction { Tx = tx }.Publish().ContinueWith(t => {
                if (t.Result == BlockChain.BlockChain.TxResultEnum.Accepted && _BroadcastHubBehavior != null)
					_BroadcastHubBehavior.BroadcastHub.BroadcastTransactionAsync(tx);

                return t.Result;
			});
		}

		/// <summary>
		/// Transmits a block on the network.
		/// </summary>
		public void Transmit(Types.Block bk)
		{
            if (_MinerBehavior != null)
	    		_MinerBehavior.BroadcastBlock(bk);
		}
	}
}