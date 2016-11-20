using System;
using System.Threading;
using System.Threading.Tasks;
using Open.Nat;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using Infrastructure;
using NBitcoin;

namespace NodeCore
{
	public class NATManager : Singleton<NATManager>
	{
		private const int TIMEOUT = 5000;
		private const string MAPPING_DESC = "Node Tester Lease (auto)";

		public IPAddress ExternalIPAddress { get; private set; }
		public IPAddress InternalIPAddress { get; private set; }
		public bool DeviceFound { get; private set; }
		public bool HasError { get; private set; }
		public bool? Mapped { get; private set; }
		public bool? ExternalIPVerified { get; private set; }

		private NatDevice _NatDevice;
		private SemaphoreSlim _SemaphoreSlim = new SemaphoreSlim(1);

		public NATManager() //TODO: check internet connection is active
		{
			IPAddress[] PrivateIPs = Utils.GetAllLocalIPv4();

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
		}

		public async Task Init()
		{
			Mapped = null;
			ExternalIPVerified = null;

			await GetNatDeviceAsync();

			if (_NatDevice != null)
			{
				ExternalIPVerified = VerifyExternalIP().Result;

				if (ExternalIPVerified.Value)
				{
					Mapped = EnsureMapping().Result;
				}
			}
		}

		public async Task<NatDevice> GetNatDeviceAsync()
		{
			var nat = new NatDiscoverer();
			var cts = new CancellationTokenSource(TIMEOUT);

			if (_NatDevice != null)
			{
				return _NatDevice;
			}

			await _SemaphoreSlim.WaitAsync();
			Trace.Information("NAT Device discovery started");

			return await nat.DiscoverDeviceAsync(PortMapper.Upnp, cts).ContinueWith(t =>
			{
				_SemaphoreSlim.Release();
				DeviceFound = t.Status != TaskStatus.Faulted;

				if (!DeviceFound)
				{
					Trace.Information("NAT Device not found");

					HasError = !(t.Exception.InnerException is NatDeviceNotFoundException);

					if (HasError)
					{
						Trace.Error("NAT Device discovery", t.Exception);
					}
					return null;
				}
				else
				{
					_NatDevice = t.Result;
				}

				return _NatDevice;
			});
		}

		public async Task<bool> VerifyExternalIP()
		{
			Trace.Information("VerifyExternalIP");

			var device = await NATManager.Instance.GetNatDeviceAsync();

			return device == null ? false : await device.GetExternalIPAsync().ContinueWith(t =>
			{
				if (t.IsFaulted)
				{
					Trace.Error("GetExternalIP", t.Exception);
					return false;
				}

				ExternalIPAddress = t.Result;

				if (ExternalIPAddress == null)
				{
					return false;
				}

				//try
				//{
				//	IPAddress ipAddress = ExternalTestingServicesHelper.GetExternalIPAsync().Result;

				//	bool match = ipAddress.Equals(ExternalIPAddress);

				//	Trace.Information("External IP " + (match ? "match" : "do not match"));

				//	return match;
				//}
				//catch (Exception e)
				//{
				//	Trace.Error("GetExternalIP", e);
				//	return false;
				//}
				return ExternalIPAddress.IsRoutable(false);
			});
		}

		public async Task<bool> EnsureMapping() {
			Trace.Information("EnsureMapping");

			var device = await NATManager.Instance.GetNatDeviceAsync();

			return device == null ? false : await device.GetSpecificMappingAsync(Protocol.Tcp, JsonLoader<Settings>.Instance.Value.ServerPort).ContinueWith(t =>
			{
				if (t.IsFaulted)
				{
					Trace.Error("GetExternalIP", t.Exception);
					return false;
				}

				var mapping = t.Result;

				try
				{
					if (mapping != null && !mapping.PrivateIP.Equals(InternalIPAddress))
					{
						Trace.Information($"existing mapping mismatch. got: {mapping.PrivateIP}, need: {InternalIPAddress}");

						_NatDevice.DeletePortMapAsync(mapping).Wait();
						mapping = null;
					}

					if (mapping == null)
					{
						Trace.Information($"creaing mapping with IP: {InternalIPAddress}");

						_NatDevice.CreatePortMapAsync(
							new Mapping(
								Protocol.Tcp,
								InternalIPAddress,
								JsonLoader<Settings>.Instance.Value.ServerPort,
								JsonLoader<Settings>.Instance.Value.ServerPort,
								0, //TODO: session lifetime?
								MAPPING_DESC
							)
						).Wait();
					}

					IEnumerable<Mapping> exisintMappings = _NatDevice.GetAllMappingsAsync().Result;

					return exisintMappings.Count(exisintMapping => exisintMapping.PublicPort == JsonLoader<Settings>.Instance.Value.ServerPort) == 1;
				}
				catch (Exception e)
				{
					Trace.Error("Mapping", e);
					return false;
				}
			});
		}
	}
}
