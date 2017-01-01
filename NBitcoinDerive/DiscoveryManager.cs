//using System;
//using NBitcoin.Protocol;
//using NBitcoin.Protocol.Behaviors;
//using System.Threading;
//using Infrastructure;
//using System.Net;
//using NBitcoin;

//namespace NBitcoinDerive
//{
//	public class DiscoveryManager : ResourceOwner
//	{
//		protected NodesGroup nodesGroup = null;

//		public struct NodeInfo {
//			public String Address;
//			public String PeerAddress;
//		}

//		public bool IsRunning { 
//			get {
//				return nodesGroup != null;
//			}
//		}

//		public void Stop()
//		{
//			if (nodesGroup != null) {
//				nodesGroup.Dispose ();
//				NodeServerTrace.Information ("Discovery stopped");
//			}
//		}

//		public void Start (Network network, int peersToFind, int maximumNodeConnection, NodeConnectionParameters parameters, IPEndPoint externalIPEndpoint = null) {
//			if (IsRunning) {
//				Stop ();
//			}

//			NBitcoin.Protocol.AddressManager addressManager = new NBitcoin.Protocol.AddressManager ();
//			addressManager.PeersToFind = peersToFind;

//			var addressManagerBehavior = new AddressManagerBehavior (addressManager);
//			parameters.TemplateBehaviors.Add(addressManagerBehavior);

//			if (externalIPEndpoint != null) {
//				//		parameters.Advertize = true;
//				addressManagerBehavior.Mode = AddressManagerBehaviorMode.AdvertizeDiscover;
//				parameters.AddressFrom = externalIPEndpoint;
//			} else {
//				addressManagerBehavior.Mode = AddressManagerBehaviorMode.Discover;
//			}

//			nodesGroup = new NodesGroup(network, parameters);
//			OwnResource (nodesGroup);

//			nodesGroup.AllowSameGroup = true; //TODO
//			nodesGroup.MaximumNodeConnection = maximumNodeConnection;

//			nodesGroup.ConnectedNodes.Added += (object sender, NodeEventArgs e) => {
//				NodeServerTrace.Information("Peer found: " + e.Node.RemoteSocketAddress + ":" + e.Node.RemoteSocketPort);
//			};
				
//			nodesGroup.Connect ();
//		}
//	}
//}

