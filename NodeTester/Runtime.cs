using System;
using Infrastructure;
using NBitcoinDerive;

namespace NodeTester
{
	public class Runtime : Singleton<Runtime> 
	{
		public interface IRuntimeMessage { }
		public class PeersSummaryMessage : IRuntimeMessage { public int Count { get; set; } }
		public class ServerSummaryMessage : IRuntimeMessage { public bool IsRunning { get; set; } }

		static LogMessageContext logMessageContext = new LogMessageContext("App");

		static private void PushMessage(IRuntimeMessage message)
		{
			Infrastructure.MessageProducer<IRuntimeMessage>.Instance.PushMessage(message);
		}

		public void Configure(IResourceOwner resourceOwner)
		{
			logMessageContext.Create ("Configure start");

			NATManager.Instance.Init ().ContinueWith (t => {
				String message = String.Empty;

				if (!NATManager.Instance.DeviceFound) {
					message += "\nNo device found";
				}
				if (NATManager.Instance.HasError) {
					message += "\nError discovering device";
				}
				if (NATManager.Instance.Mapped.HasValue && !NATManager.Instance.Mapped.Value) {
					message += "\nCould not create mapping";
				}
				if (NATManager.Instance.ExternalIPAddress == null) {
					message += "\nCould not get discover IP";
				}
				if (NATManager.Instance.ExternalIPVerified.HasValue && !NATManager.Instance.ExternalIPVerified.Value) {
					message += /*"\nExternal IP address mismatch"*/ "\nExternal IP is not routable";
				}

				if (message != String.Empty) {
					logMessageContext.Create (message);
				}

				if (NATManager.Instance.DeviceFound &&
					NATManager.Instance.Mapped.Value &&
					NATManager.Instance.ExternalIPVerified.Value) {

					try {
						ServerManager.Instance.Start (resourceOwner, NATManager.Instance.ExternalIPAddress);
					} catch (Exception e) {
						NodeTester.Trace.Error ("Error starting server", e);
					}

					PushMessage (new ServerSummaryMessage () { IsRunning = NodeTester.ServerManager.Instance.IsListening });
				}

				if (JsonLoader<Settings>.Instance.Value.IPSeeds.Count == 0) {
					logMessageContext.Create ("No seeds defined");
				} else {
					NodeTester.DiscoveryManager.Instance.Start (resourceOwner, NATManager.Instance.ExternalIPAddress);
				}
			});
		}
	}
}

