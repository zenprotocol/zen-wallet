using System;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using System.Net;
using NBitcoin.Protocol.Filters;
using Infrastructure;
using NodeCore;

namespace NodeCore
{
	public class ServerManager : Singleton<ServerManager>
	{
		private NodeServer Server = null;

		public bool IsRunning { 
			get {
				return Server != null && Server.IsListening;
			}
		}

		public void Stop() {
			if (Server != null) {
				Trace.Information ("Server Stopped");
				Server.Dispose ();
			}
		}

		public void Start(IResourceOwner resourceOwner, IPAddress ExternalAddress)
		{
			Start(resourceOwner, new IPEndPoint(ExternalAddress, JsonLoader<Settings>.Instance.Value.ServerPort));
		}

		public void Start (IResourceOwner resourceOwner, IPEndPoint ExternalEndpoint)
		{
			if (IsRunning) {
				Stop ();
			}

			NBitcoin.Network network = TestNetwork.Instance;

			Server = new NodeServer(network, internalPort: JsonLoader<Settings>.Instance.Value.ServerPort);
			resourceOwner.OwnResource (Server);

			InitHandlers ();
			InitParameters ();

			Server.AllowLocalPeers = true; //TODO
			Server.ExternalEndpoint = ExternalEndpoint;

			Server.Listen ();

			Trace.Information ("Server " + (Server.IsListening ? "listening" : "not listening"));
		}

		private void InitParameters() {
			NodeConnectionParameters nodeConnectionParameters = new NodeConnectionParameters ();
			NBitcoin.Protocol.AddressManager addressManager = AddressManager.Instance.GetBitcoinAddressManager (); // new NBitcoin.Protocol.AddressManager ();

			nodeConnectionParameters.TemplateBehaviors.Add (new AddressManagerBehavior (addressManager));
			Server.InboundNodeConnectionParameters = nodeConnectionParameters;
		}

		protected virtual void InitHandlers() {
		}
	}
}

