using System.Threading;
using System.Threading.Tasks;
using Open.Nat;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System;
using Infrastructure;
using NBitcoin;
using NBitcoinDerive;

namespace NodeTester
{
	public class NATTestsHelper : Singleton<NATTestsHelper>
	{
		private static LogMessageContext logMessageContext = new LogMessageContext("NAT Tests");
		private const int TIMEOUT = 5000;

		public async Task<IPAddress> GetExternalIPAsync ()
		{
			logMessageContext.Create ("Getting IP");

			var device = await App.NodeManager._NATManager.GetNatDeviceAsync();

			if (device == null) {
				logMessageContext.Create ("device not found");
			}

			return device == null ? null : await device.GetExternalIPAsync ().ContinueWith (t => {
				if (t.IsFaulted) {
					Trace.Error("GetExternalIP", t.Exception);
					return null;
				}

				String ipAdressInvestigate = Utils.IPAdressInvestigate(t.Result);

				if (ipAdressInvestigate != null)
				{
					logMessageContext.Create(ipAdressInvestigate);
				}

				return t.Result;
			});
		}

		public async Task<IEnumerable<Mapping>> GetAllMappingsAsync()
		{
			logMessageContext.Create ("Getting mapping");

			var device = await App.NodeManager._NATManager.GetNatDeviceAsync();

			if (device == null) {
				logMessageContext.Create ("device not found");
			}

			return device == null ? null : await device.GetAllMappingsAsync ().ContinueWith (t => {
				if (t.IsFaulted) {
					Trace.Error("GetMapping", t.Exception);
					return null;
				}

				return t.Result;
			});
		}

		public async Task<int> ListDevicesAsync(bool includePMP)
		{
			var nat = new NatDiscoverer();
			var cts = new CancellationTokenSource(TIMEOUT);

			PortMapper param = PortMapper.Upnp;

			if (includePMP) {
				param |= PortMapper.Pmp;
			}

			logMessageContext.Create ("Getting devices");

			return await nat.DiscoverDevicesAsync(param, cts).ContinueWith(t =>
			{
				int result = 0;

				if (t.Status != TaskStatus.Faulted) {
					using (IEnumerator<NatDevice> enumerator = t.Result.GetEnumerator())
					{
						while (enumerator.MoveNext()) {
							logMessageContext.Create ("Found device: " + enumerator.Current.ToString());
							result++;
						}
					}
				} else {
					if (!(t.Exception.InnerException is NatDeviceNotFoundException))
					{
						result = -1;
						logMessageContext.Create ("error listing devices");
					}
				}

				return result;
			});
		}

		public async Task<bool> AddMappingAsync(IPAddress PrivateIP)
		{
			logMessageContext.Create ("Adding mapping");

			var device = await App.NodeManager._NATManager.GetNatDeviceAsync();

			if (device == null) {
				logMessageContext.Create ("device not found");
			}

			Network network = JsonLoader<Network>.Instance.Value;

			string desc = "Node Tester Lease (manual)";

			return await device.CreatePortMapAsync(
				new Mapping(
					Protocol.Tcp,
					PrivateIP, 
					network.DefaultPort, 
					network.DefaultPort, 
					0, 
					desc
				)
			).ContinueWith(t => {
				if (t.IsFaulted) {
					logMessageContext.Create ("error creating mapping");
					Trace.Error("error creating mapping", t.Exception);
					return false;
				}
				else 
				{
					try {
						IEnumerable<Mapping> exisintMappings = device.GetAllMappingsAsync().Result;

						return exisintMappings.Count(exisintMapping => exisintMapping.Description == desc) == 1;
					} catch (Exception e) {
						Trace.Error("Verify mapping", e);
						return false;
					}
				}
			});
		}

		public async Task<bool> RemoveMappingAsync(Mapping mapping)
		{
			logMessageContext.Create ("Removing mapping");

			var device = await App.NodeManager._NATManager.GetNatDeviceAsync();

			if (device == null) {
				logMessageContext.Create ("device not found");
			}

			return device == null ? false : await device.DeletePortMapAsync(mapping).ContinueWith(t => {
				return !t.IsFaulted;
			});
		}
	}
}

