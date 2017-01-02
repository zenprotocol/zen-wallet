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
	public interface INodeManager {
		void SendTransaction (byte[] address, UInt64 amount);
	}

	public class NodeManager : ResourceOwner, INodeManager
	{
		private Server _Server = null;
		private Network _Network;
		private NodeConnectionParameters _NodeConnectionParameters;
		private BroadcastHub _TransactionBroadcastHub;

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

		public NodeManager()
		{
			_Network = JsonLoader<NBitcoinDerive.Network>.Instance.Value;

			AddressManager addressManager = new AddressManager();

			_NodeConnectionParameters = new NodeConnectionParameters();
			var addressManagerBehavior = new AddressManagerBehavior (addressManager);
			_NodeConnectionParameters.TemplateBehaviors.Add(addressManagerBehavior);
		}

		#if DEBUG
		public async Task Start(bool lanMode = false, bool disableInboundMode = false)
		#else
		public async Task Start()
		#endif
		{
			_NATManager = new NATManager(_Network.DefaultPort);

			#if DEBUG
			if (lanMode)
			{
				StartNode(_NATManager.InternalIPAddress, disableInboundMode);
				return;
			}
			#endif

			await _NATManager.Init ().ContinueWith (t => {
				if (_NATManager.DeviceFound &&
					 _NATManager.Mapped.Value &&
					 _NATManager.ExternalIPVerified.Value)
				{
					StartNode(_NATManager.ExternalIPAddress, _NATManager.ExternalIPVerified.Value);
				}
				else 
				{
					StartNode(null, true);
				}
			});	
		}
			
		public async Task StartNode(IPAddress ipAddress, bool disableInboundMode)
		{
			var address = ipAddress == null ? null : new System.Net.IPEndPoint (ipAddress, _Network.DefaultPort);

			InitBlockchain();

			if (!disableInboundMode) {
				_Server = new Server (address, _Network, _NodeConnectionParameters);
				OwnResource (_Server);

				if (_Server.Start ()) {
					NodeServerTrace.Information ($"Server started at {ipAddress}:{_Network.DefaultPort}");
				} else {
					NodeServerTrace.Information ($"Could not start server at {ipAddress}:{_Network.DefaultPort}");
				}
			}

			StartDiscovery();
		}
			
		private void InitBlockchain()
		{
			var p = new TestTransactionPool();

			p.Add("t1", 1);
			p.Render();

			var genesisBlock = new TestBlock(p.TakeOut("t1").Value);
			genesisBlock.Render();

			var blockChain = new BlockChain.BlockChain("db", genesisBlock.Value.Key);
			OwnResource (blockChain);

			blockChain.OnAddedToMempool += transaction => {
				PushMessage (new TransactionAddToMempoolMessage () { Transaction = transaction });
			};

			blockChain.OnAddedToStore += transaction =>
			{
				PushMessage(new TransactionAddToStoreMessage() { Transaction = transaction });
			};

			BroadcastHubBehavior broadcastHubBehavior = new BroadcastHubBehavior();
			_TransactionBroadcastHub = broadcastHubBehavior.BroadcastHub;

			Miner miner = new Miner(blockChain);

			_NodeConnectionParameters.TemplateBehaviors.Add(new MinerBehavior(miner));
			_NodeConnectionParameters.TemplateBehaviors.Add(new SPVBehavior(blockChain, _TransactionBroadcastHub));
			_NodeConnectionParameters.TemplateBehaviors.Add(new ChainBehavior(blockChain));
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


				_TransactionBroadcastHub.BroadcastTransactionAsync(transaction);
			
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}
	}
}