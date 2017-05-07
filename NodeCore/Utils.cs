using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NodeCore
{
	public class Utils
	{
		public static IPEndPoint ParseIPEndPoint(string endPoint) // can be an extention method
		{
			string[] ep = endPoint.Split(':');
			if (ep.Length < 2) throw new FormatException("Invalid endpoint format");
			IPAddress ip;
			if (ep.Length > 2)
			{
				if (!IPAddress.TryParse(string.Join(":", ep, 0, ep.Length - 1), out ip))
				{
					throw new FormatException("Invalid ip-adress");
				}
			}
			else
			{
				if (!IPAddress.TryParse(ep[0], out ip))
				{
					throw new FormatException("Invalid ip-adress");
				}
			}
			int port;
			if (!int.TryParse(ep[ep.Length - 1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
			{
				throw new FormatException("Invalid port");
			}
			return new IPEndPoint(ip, port);
		}

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
	}
}

