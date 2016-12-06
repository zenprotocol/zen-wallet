//using System;
//using NBitcoin.Protocol;
//using NBitcoin.Protocol.Behaviors;
//using NBitcoin;
//using System.Net;
//using System.Threading;
//using Infrastructure;
//using NodeCore;

//namespace NodeCore
//{
//	public class DiscoveryManager : Singleton<DiscoveryManager>
//	{
//		protected NodesGroup nodesGroup = null;
	
//		public bool IsRunning { 
//			get {
//				return nodesGroup != null;
//			}
//		}

//		public void Stop() {
//			if (nodesGroup != null) {
//				nodesGroup.Dispose ();
//				Trace.Information ("Discovery stopped");
//			}
//		}


//		public void Start (IResourceOwner resourceOwner, IPAddress ExternalIPAddress = null) {
//			if (IsRunning) {
//				Stop ();
//			}

//			Trace.Information ("Discovery started");

//			Settings Settings = JsonLoader<Settings>.Instance.Value;

//			var network = new TestNetwork ();

//			NBitcoin.Protocol.AddressManager addressManager = new NBitcoin.Protocol.AddressManager ();
//			addressManager.PeersToFind = Settings.PeersToFind;

//			NodeConnectionParameters parameters = new NodeConnectionParameters();
//			var addressManagerBehavior = new AddressManagerBehavior(addressManager);
//			parameters.TemplateBehaviors.Add(addressManagerBehavior);

//			WalletManager.Instance.Setup(parameters.TemplateBehaviors);
	
//			if (ExternalIPAddress != null)
//			{
//				//		parameters.Advertize = true;
//				addressManagerBehavior.Mode = AddressManagerBehaviorMode.AdvertizeDiscover;
//				parameters.AddressFrom = new IPEndPoint(ExternalIPAddress, JsonLoader<Settings>.Instance.Value.ServerPort);
//			}
//			else {
//				addressManagerBehavior.Mode = AddressManagerBehaviorMode.Discover;
//			}

//			nodesGroup = new NodesGroup(network, parameters);
//			resourceOwner.OwnResource (nodesGroup);

//			nodesGroup.AllowSameGroup = true; //TODO
//			nodesGroup.MaximumNodeConnection = Settings.MaximumNodeConnection;

////			nodesGroup.ConnectedNodes.Added += (object sender, NodeEventArgs e) => {
////				PushMessage(new PeerFoundMessage() { 
////					NodeInfo = new NodeInfo() { Address = e.Node.Peer.Endpoint.ToString() }
////				});
////				LogMessageContext.Create("Peer found: " + e.Node.RemoteSocketAddress + ":" + e.Node.RemoteSocketPort);
////
////				Infrastructure.MessageProducer<App.ITesterMessage>.Instance.PushMessage(new App.PeersSummaryMessage() { Count = nodesGroup.ConnectedNodes.Count });
////			};
				
//			nodesGroup.Connect ();

////			stopWatcherThread = false;
////			new Thread (() => {
////				while (!stopWatcherThread) {
////					if (nodesGroup.ConnectedNodes.Count == Settings.PeersToFind) {
////						PushMessage (new DoneMessage ());
////						LogMessageContext.Create ("Done");
////						return;
////					}
////
////					Thread.Sleep (100);
////				}
////			}).Start ();
////			resourceOwner.OwnResource (this); //only to stop the thread
//		}
//	}
//}

