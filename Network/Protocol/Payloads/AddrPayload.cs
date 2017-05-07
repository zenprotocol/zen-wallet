#if !NOSOCKET
using System;
using System.Linq;
using MsgPack;
namespace NBitcoin.Protocol
{
	/// <summary>
	/// An available peer address in the bitcoin network is announce (unsollicited or after a getaddr)
	/// </summary>
	public class AddrPayload
	{
		NetworkAddress[] addr_list = new NetworkAddress[0];

		public NetworkAddress[] Addresses
		{
			get
			{
				return addr_list;
			}
		}

		public AddrPayload()
		{

		}

		public AddrPayload(NetworkAddress[] addresses)
		{
			addr_list = addresses.ToArray();
		}

		public override string ToString()
		{
#if TRACE
			return Addresses.Length + " address(es): " + (Addresses.Length == 0 ? "" : String.Join(",", Addresses.Select((arg) => arg.Endpoint.ToString())));
#else
			return Addresses.Length + " address(es)";
#endif
		}
	}
}
#endif