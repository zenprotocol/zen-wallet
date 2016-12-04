using System;
using Infrastructure;
using System.Threading.Tasks;
using System.Net;
using System.Linq;
using NodeCore;

namespace NodeTester
{
	public class LanNodeManager : INodeManager
	{
		private Server _Server = null;
		
		public async Task Start (IResourceOwner resourceOwner, NBitcoin.Network network)
		{
			IPAddress[] PrivateIPs = NodeCore.Utils.GetAllLocalIPv4();
			IPAddress InternalIPAddress;

			if (PrivateIPs.Count() == 0)
			{
				Trace.Information("Warning, local addresses not found");
				InternalIPAddress = null;
			}
			else {
				InternalIPAddress = PrivateIPs.First();

				if (PrivateIPs.Count() > 1)
				{
					Trace.Information("Warning, found " + PrivateIPs.Count() + " internal addresses");
				}
			}

			var ipEndpoint = new System.Net.IPEndPoint(InternalIPAddress, JsonLoader<Settings>.Instance.Value.ServerPort);

			_Server = new Server(resourceOwner, ipEndpoint, network);

			if (_Server.Start())
			{
				Trace.Information($"Server started at {ipEndpoint}");
			}
			else
			{
				Trace.Information($"Could not start server at {ipEndpoint}");
			}

			if (JsonLoader<Settings>.Instance.Value.IPSeeds.Count == 0) {
				Trace.Information ("No seeds defined");
			} else {
				DiscoveryManager.Instance.Start (resourceOwner, InternalIPAddress);
			}
		}

		public void Stop() {
			_Server.Stop ();
		//	DiscoveryManager.Instance.Stop ();
		}
	}
}