using System;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using NBitcoin;
using System.Net;
using System.Threading;
using Infrastructure;
using NodeCore;
using NodeTester;

namespace NodeTester
{
	public class DiscoveryManager : Singleton<DiscoveryManager>, IDisposable
	{
		protected NodesGroup nodesGroup = null;

		public struct NodeInfo {
			public String Address;
			public String PeerAddress;
		}

		public bool IsRunning { 
			get {
				return nodesGroup != null;
			}
		}

		public interface IMessage {}

		public class StartedMessage : IMessage {}
		public class StoppedMessage : IMessage {}
		public class DoneMessage : IMessage {}
		public class ErrorMessage : IMessage {}
		public class PeerFoundMessage : IMessage { public NodeInfo NodeInfo { get; set; } }
					
		LogMessageContext LogMessageContext = new LogMessageContext("Discovery");

		bool stopWatcherThread = false;

		private void PushMessage(IMessage Message) {
			Infrastructure.MessageProducer<IMessage>.Instance.PushMessage (Message);
		}

		public void Stop()
		{
			if (nodesGroup != null) {
				nodesGroup.Dispose ();
				Trace.Information ("Discovery stopped");
			}
		}

		public void Dispose () {
			stopWatcherThread = true;
		}

		public void Start (IResourceOwner resourceOwner, IPAddress ExternalIPAddress = null) {
			if (IsRunning) {
				Stop ();
			}

			PushMessage (new StartedMessage ()); 
			LogMessageContext.Create ("Started");

			Settings Settings = JsonLoader<Settings>.Instance.Value;

			var network = new TestNetwork ();

			NBitcoin.Protocol.AddressManager addressManager = new NBitcoin.Protocol.AddressManager ();
			addressManager.PeersToFind = Settings.PeersToFind;

			NodeConnectionParameters parameters = new NodeConnectionParameters();
			parameters.TemplateBehaviors.Add(new AddressManagerBehavior(addressManager));

			WalletManager.Instance.Setup(parameters.TemplateBehaviors);

			if (ExternalIPAddress != null)
			{
				parameters.Advertize = true;
				parameters.AddressFrom = new IPEndPoint(ExternalIPAddress, JsonLoader<Settings>.Instance.Value.ServerPort);
			}

			nodesGroup = new NodesGroup(network, parameters);
			resourceOwner.OwnResource (nodesGroup);

			nodesGroup.AllowSameGroup = true; //TODO
			nodesGroup.MaximumNodeConnection = Settings.MaximumNodeConnection;

			nodesGroup.ConnectedNodes.Added += (object sender, NodeEventArgs e) => {
				PushMessage(new PeerFoundMessage() { 
					NodeInfo = new NodeInfo() { Address = e.Node.Peer.Endpoint.ToString() }
				});
				LogMessageContext.Create("Peer found: " + e.Node.RemoteSocketAddress + ":" + e.Node.RemoteSocketPort);

				//Infrastructure.MessageProducer<ITesterMessage>.Instance.PushMessage(new App.PeersSummaryMessage() { Count = nodesGroup.ConnectedNodes.Count });
			};
				
			nodesGroup.Connect ();

			stopWatcherThread = false;
			new Thread (() => {
				while (!stopWatcherThread) {
					if (nodesGroup.ConnectedNodes.Count == JsonLoader<Settings>.Instance.Value.PeersToFind) {
						PushMessage (new DoneMessage ());
						LogMessageContext.Create ("Done");
						return;
					}

					Thread.Sleep (100);
				}
			}).Start ();
			resourceOwner.OwnResource (this); //only to stop the thread
		}

		public void SendTransaction(/*Consensus.Types.Transaction transaction*/)
		{
			Console.WriteLine("sending a transaction");

			var hub = BroadcastHub.GetBroadcastHub(nodesGroup.NodeConnectionParameters);

			hub.BroadcastTransactionAsync(Consensus.Tests.tx);
		}
	}
}

