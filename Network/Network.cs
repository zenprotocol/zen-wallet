//using System;
//using System.Collections.Generic;
//using System.Net;
//using NBitcoin.Protocol;

//namespace NBitcoin
//{
//	public interface Network
//	{
//		IEnumerable<DNSSeedData> DNSSeeds { get; }
//		IEnumerable<NetworkAddress> SeedNodes { get; }
//		int DefaultPort { get; }
//		uint Magic { get; }
//	}

//	public class DNSSeedData
//	{
//		string name, host;
//		public string Name
//		{
//			get
//			{
//				return name;
//			}
//		}
//		public string Host
//		{
//			get
//			{
//				return host;
//			}
//		}
//		public DNSSeedData(string name, string host)
//		{
//			this.name = name;
//			this.host = host;
//		}
//#if !NOSOCKET
//		IPAddress[] _Addresses = null;
//		public IPAddress[] GetAddressNodes()
//		{
//			if(_Addresses != null)
//				return _Addresses;
//			try
//			{
//				_Addresses = Dns.GetHostAddressesAsync(host).Result;
//			}
//			catch(AggregateException ex)
//			{
//				System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
//			}
//			return _Addresses;
//		}
//#endif
//		public override string ToString()
//		{
//			return name + " (" + host + ")";
//		}
//	}
//}
