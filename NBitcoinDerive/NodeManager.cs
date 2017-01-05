using System;
using Infrastructure;
using System.Threading.Tasks;
using NBitcoinDerive;
using NBitcoin.Protocol;
using System.Collections.Generic;
using NBitcoin;
using NBitcoin.Protocol.Behaviors;
using System.Net;
using Infrastructure.Testing.Blockchain;
using Consensus;
using Microsoft.FSharp.Collections;

namespace NBitcoinDerive
{
	public class EndpointOptions {
		public enum EndpointOptionsEnum {
			#if DEBUG
			UseInternalIP,
			UseNone,
			#endif
			UseUPnP,
			UseSpecified
		}

		public EndpointOptionsEnum EndpointOption { get; set; }
		public IPAddress SpecifiedAddress { get; set; }
	}

	public interface INodeManager {
		void SendTransaction (byte[] address, UInt64 amount);
	}

	public class NodeManager : ResourceOwner, INodeManager
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

		public interface IMessage { }
		public class TransactionAddToMempoolMessage : IMessage { public Types.Transaction Transaction { get; set; } }
		public class TransactionAddToStoreMessage : IMessage { public Types.Transaction Transaction { get; set; } }

		private void PushMessage(IMessage message)
		{
			Infrastructure.MessageProducer<IMessage>.Instance.PushMessage(message);
		}

		public NodeManager(BlockChain.BlockChain blockChain, EndpointOptions endpointOptions)
		{
			_BlockChain = blockChain;
			_Network = JsonLoader<NBitcoinDerive.Network>.Instance.Value;

			AddressManager addressManager = new AddressManager();

			_NodeConnectionParameters = new NodeConnectionParameters();
			var addressManagerBehavior = new AddressManagerBehavior (addressManager);
			_NodeConnectionParameters.TemplateBehaviors.Add(addressManagerBehavior);

			_NATManager = new NATManager(_Network.DefaultPort);

			switch (endpointOptions.EndpointOption) 
			{
			case EndpointOptions.EndpointOptionsEnum.UseInternalIP:
				StartNode(_NATManager.InternalIPAddress);
				break;
			case EndpointOptions.EndpointOptionsEnum.UseNone:
				StartNode();
				break;
			case EndpointOptions.EndpointOptionsEnum.UseSpecified:
				StartNode (endpointOptions.SpecifiedAddress);
				break;
			case EndpointOptions.EndpointOptionsEnum.UseUPnP:
				_NATManager.Init ().ContinueWith (t => {
					if (_NATManager.DeviceFound &&
						_NATManager.Mapped.Value &&
						_NATManager.ExternalIPVerified.Value)
					{
						StartNode(_NATManager.ExternalIPAddress);
					}
					else 
					{
						StartNode();
					}
				});	

				break;
			}
		}
			
		public async Task StartNode(IPAddress externalAddress = null)
		{
			BroadcastHubBehavior broadcastHubBehavior = new BroadcastHubBehavior();

			Miner miner = new Miner(_BlockChain);

			_NodeConnectionParameters.TemplateBehaviors.Add(broadcastHubBehavior);
			_NodeConnectionParameters.TemplateBehaviors.Add(new MinerBehavior(miner));
			_NodeConnectionParameters.TemplateBehaviors.Add(new SPVBehavior(_BlockChain, broadcastHubBehavior.BroadcastHub));
			_NodeConnectionParameters.TemplateBehaviors.Add(new ChainBehavior(_BlockChain));

			_BlockChain.OnAddedToMempool += t => {
				broadcastHubBehavior.BroadcastHub.BroadcastTransactionAsync(t);
			};

			if (externalAddress != null) {
				_Server = new Server (externalAddress, _Network, _NodeConnectionParameters);
				OwnResource (_Server);

				if (_Server.Start ()) {
					NodeServerTrace.Information ($"Server started at {externalAddress}:{_Network.DefaultPort}");
				} else {
					NodeServerTrace.Information ($"Could not start server at {externalAddress}:{_Network.DefaultPort}");
				}
			}

			StartDiscovery ();
		}
									
		#if DEBUG
		public
		#else 
		private 
		#endif
		void StartDiscovery() 
		{
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
			else {
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

		private Random _Random = new Random();

		public void SendTransaction(byte[] address, UInt64 amount)
		{
			try
			{
				var outputs = new List<Types.Output>();

				var pklock = Types.OutputLock.NewPKLock(address);
				outputs.Add(new Types.Output(pklock, new Types.Spend(Tests.zhash, amount)));

				var inputs = new List<Types.Outpoint>();

				//	inputs.Add(new Types.Outpoint(address, 0));

				var hashes = new List<byte[]>();

				//hack Concensus into giving a different hash per each tx created
				var version = (uint)_Random.Next(1000);

				Types.Transaction transaction = new Types.Transaction(version,
					ListModule.OfSeq(inputs),
					ListModule.OfSeq(hashes),
					ListModule.OfSeq(outputs),
					null);

				Consensus.Merkle.transactionHasher.Invoke(transaction);

				_BlockChain.HandleNewTransaction(transaction);
			
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}
	}
}