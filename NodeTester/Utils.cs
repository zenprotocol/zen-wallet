using System;
using System.Net;
using System.Globalization;
using NBitcoin.Protocol;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;
using Gtk;

namespace NodeTester
{
	public static class Extensions
	{
		public static void ShowMessage(this Window window, String message, MessageType messageType = MessageType.Info)
		{
			Gtk.Application.Invoke (delegate {
				MessageDialog md = new MessageDialog (
					window,
					DialogFlags.DestroyWithParent,
					messageType,
					ButtonsType.Close, message
				);

				md.Run ();
				md.Destroy ();
			});		
		}
	}  

	public class Utils
	{
		public static IPAddress[] GetAllLocalIPv4()
		{
			List<IPAddress> ipAddrList = new List<IPAddress>();

			foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (item.OperationalStatus != OperationalStatus.Up)
				{
					continue;
				}

				//TODO: which ones..?
				if (item.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
					item.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
					item.NetworkInterfaceType == NetworkInterfaceType.Ethernet3Megabit ||
					item.NetworkInterfaceType == NetworkInterfaceType.FastEthernetFx ||
					item.NetworkInterfaceType == NetworkInterfaceType.FastEthernetT ||
					item.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet)
				{
					foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
					{
						if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
						{
							ipAddrList.Add(ip.Address);
						}
					}
				}
			}

			return ipAddrList.ToArray();
		}

		public static String GetPayloadContent(System.Object payload) {
			String returnValue = payload.ToString ();

			if (payload is AddrPayload) {
				foreach (NetworkAddress networkAddress in ((AddrPayload) payload).Addresses) {
					returnValue += "\n" + networkAddress.Endpoint;
				}
			}

			return returnValue;
		}

		public static String IPAdressInvestigate(IPAddress address)
		{
			List<String> results = new List<string>();

			Action<bool, String> Add = (arg1, arg2) =>
			{
				results.Add(arg2 + ": " + arg1);
			};

			Add(address.IsValid(), "IsValid");
			Add(address.IsRFC6264(), "IsRFC6264");
			Add(address.IsRFC1918(), "IsRFC1918");
			Add(address.IsRFC3927(), "IsRFC3927");
			Add(address.IsRFC4862(), "IsRFC4862");
			Add(address.IsRFC4193(), "IsRFC4193");
			Add(address.IsRFC4843(), "IsRFC4843");
			Add(address.IsRFC3849(), "IsRFC3849");
			Add(address.IsRFC3964(), "IsRFC3964");
			Add(address.IsRFC4380(), "IsRFC4380");
			Add(address.IsRFC6052(), "IsRFC6052");
			Add(address.IsRFC6145(), "IsRFC6145");
			Add(address.IsTor(), "IsTor");
			Add(address.IsIPv4(), "IsIPv4");
			Add(address.IsIPv6Teredo, "IsIPv6Teredo");
			Add(address.IsIPv6LinkLocal, "IsIPv6LinkLocal");
			Add(address.IsIPv6Multicast, "IsIPv6Multicast");
			Add(address.IsIPv6SiteLocal, "IsIPv6SiteLocal");
			Add(address.IsIPv4MappedToIPv6Ex(), "IsIPv4MappedToIPv6Ex");
			Add(address.IsLocal(), "IsLocal");
			Add(address.IsMulticast(), "IsMulticast");
			Add(address.IsRoutable(false), "IsRoutable(local=false)");
			Add(address.IsRoutable(true), "IsRoutable(local=true)");

			return $"IP Adress Investigation results for {address}:\n\n" + String.Join("\n", results.ToArray());
		}
	}
}

