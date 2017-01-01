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

namespace NBitcoinDerive
{
	public class NodeManager : ResourceOwner
	{
		private Server _Server = null;
		private NATManager _NATManager;
	//	private BlockChain.BlockChain _BlockChain;
		private Network _Network;
	//	private Settings _Settings;
		private NodeConnectionParameters _NodeConnectionParameters;
		private AddressManager _AddressManager; 

		public NodeManager()
		{
	//		_BlockChain = blockChain;
			_Network = JsonLoader<NBitcoinDerive.Network>.Instance.Value;
		//	_Settings = settings;
			_NodeConnectionParameters = new NodeConnectionParameters();
			_AddressManager = new AddressManager();
		}


		#if DEBUG
		public async Task Start(bool lanMode = false)
		#else
		public async Task Start()
		#endif
		{
			_NATManager = new NATManager(_Network.DefaultPort);

			#if DEBUG
			if (lanMode)
			{
				StartNode(_NATManager.InternalIPAddress);
				return;
			}
			#endif

			await _NATManager.Init ().ContinueWith (t => {
				if (_NATManager.DeviceFound &&
					 _NATManager.Mapped.Value &&
					 _NATManager.ExternalIPVerified.Value)
				{
					StartNode(_NATManager.ExternalIPAddress);
				}
			});	
		}

		private void StartNode(IPAddress ipAddress)
		{
			_Server = new Server(new System.Net.IPEndPoint(ipAddress, _Network.DefaultPort), _Network, _AddressManager);
			OwnResource(_Server);

			if (_Server.Start())
			{
				NodeServerTrace.Information($"Server started at {ipAddress}:{_Network.DefaultPort}");
			}
			else
			{
				NodeServerTrace.Information($"Could not start server at {ipAddress}:{_Network.DefaultPort}");
			}

			InitBlockchain();
			StartDiscovery();
		}

		private void InitBlockchain()
		{
			var p = new TestTransactionPool();

			p.Add("t1", 1);
		//	p.Add("t2", 0);
		//	p.Add("t3", 0);
		//	p.Spend("t2", "t1", 0);

			p.Render();

			var genesisBlock = new TestBlock(p.TakeOut("t1").Value);
			//var block1 = new TestBlock(p.TakeOut("t2").Value, p.TakeOut("t3").Value);
			//block1.Parent = genesisBlock;

			genesisBlock.Render();
			//block1.Render();

			var blockChain = new BlockChain.BlockChain("db", genesisBlock.Value.Key);
			OwnResource (blockChain);

			BroadcastHubBehavior broadcastHubBehavior = new BroadcastHubBehavior();
			BroadcastHub broadcastHub = broadcastHubBehavior.BroadcastHub;

			Miner miner = new Miner(blockChain);

			_NodeConnectionParameters.TemplateBehaviors.Add(new MinerBehavior(miner));
			_NodeConnectionParameters.TemplateBehaviors.Add(new SPVBehavior(blockChain, broadcastHub));
			_NodeConnectionParameters.TemplateBehaviors.Add(new ChainBehavior(blockChain));
		}

		private void StartDiscovery() 
		{
			if (_Network.Seeds.Count == 0)
			{
				NodeServerTrace.Information("No seeds defined");
				return;
			}

			_AddressManager.PeersToFind = _Network.PeersToFind;

			var addressManagerBehavior = new AddressManagerBehavior(_AddressManager);
			_NodeConnectionParameters.TemplateBehaviors.Add(addressManagerBehavior);

			if (_NATManager.ExternalIPAddress != null)
			{
				addressManagerBehavior.Mode = AddressManagerBehaviorMode.AdvertizeDiscover; //parameters.Advertize = true;
				_NodeConnectionParameters.AddressFrom = new System.Net.IPEndPoint(_NATManager.ExternalIPAddress, _Network.DefaultPort);
			}
			else {
				addressManagerBehavior.Mode = AddressManagerBehaviorMode.Discover;
			}

			NodesGroup nodesGroup = new NodesGroup(_Network, _NodeConnectionParameters);
			OwnResource(nodesGroup);

			nodesGroup.AllowSameGroup = true; //TODO
			nodesGroup.MaximumNodeConnection = _Network.MaximumNodeConnection;

			nodesGroup.ConnectedNodes.Added += (object sender, NodeEventArgs e) =>
			{
				NodeServerTrace.Information("Peer found: " + e.Node.RemoteSocketAddress + ":" + e.Node.RemoteSocketPort);
			};

			nodesGroup.Connect();
		}
	}
}