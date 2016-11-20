using System;
using Infrastructure;
using System.Threading.Tasks;

namespace NodeCore
{
	public class App : Singleton<App>
	{
		public async Task Start (IResourceOwner resourceOwner)
		{
			await NATManager.Instance.Init ().ContinueWith (t => {
				if (NATManager.Instance.DeviceFound &&
				     NATManager.Instance.Mapped.Value &&
				     NATManager.Instance.ExternalIPVerified.Value) {

					try {
						ServerManager.Instance.Start (resourceOwner, NATManager.Instance.ExternalIPAddress);
					} catch (Exception e) {
						Trace.Error ("Error starting server", e);
					}
				}

				if (JsonLoader<Settings>.Instance.Value.IPSeeds.Count == 0) {
					Trace.Information ("No seeds defined");
				} else {
					DiscoveryManager.Instance.Start (resourceOwner, NATManager.Instance.ExternalIPAddress);
				}
			});			
		}

		public void Stop() {
			ServerManager.Instance.Stop ();
			DiscoveryManager.Instance.Stop ();
		}
	}
}