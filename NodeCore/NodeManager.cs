using System;
using Infrastructure;
using System.Threading.Tasks;

namespace NodeCore
{
	public class NodeManager
	{
		private Server _Server = null;
		
		public async Task Start (IResourceOwner resourceOwner)
		{
			await NATManager.Instance.Init ().ContinueWith (t => {
				var Settings = JsonLoader<Settings>.Instance.Value;

				if (NATManager.Instance.DeviceFound &&
				     NATManager.Instance.Mapped.Value &&
				     NATManager.Instance.ExternalIPVerified.Value) {

					var ipEndpoint = new System.Net.IPEndPoint(NATManager.Instance.ExternalIPAddress, Settings.ServerPort);

					_Server = new Server(resourceOwner, ipEndpoint);

					if (_Server.Start())
					{
						Trace.Information($"Server started at {ipEndpoint}");
					}
					else
					{
						Trace.Information($"Could not start server at {ipEndpoint}");
					}
				}

				//if (Settings.IPSeeds.Count == 0) {
				//	Trace.Information ("No seeds defined");
				//} else {
				//	DiscoveryManager.Instance.Start (resourceOwner, NATManager.Instance.ExternalIPAddress);
				//}
			});			
		}

		public void Stop() {
			_Server.Stop ();
		//	DiscoveryManager.Instance.Stop ();
		}
	}
}