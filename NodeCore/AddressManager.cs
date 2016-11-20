using System;
using NBitcoin;
using System.IO;
using NBitcoin.Protocol;
using System.Net;
using System.Collections.Generic;
using Infrastructure;

namespace NodeCore
{
	public class AddressManager : Singleton<AddressManager>
	{
//		LogMessageContext LogMessageContext = new LogMessageContext("Address Manager");
		Network network = TestNetwork.Instance;
		String addressManagerFile = "test.dat";
		NBitcoin.Protocol.AddressManager addressManager = null;

		public AddressManager ()
		{
			if (File.Exists (addressManagerFile)) {
				addressManager = NBitcoin.Protocol.AddressManager.LoadPeerFile (addressManagerFile);
//				LogMessageContext.Create ("Loaded " + Count() + " address(es)");
			} else {
				addressManager = new NBitcoin.Protocol.AddressManager ();
				addressManager.SavePeerFile (addressManagerFile, network);
//				LogMessageContext.Create ("Created");
			}
		}

		public IEnumerable<IPEndPoint> GetNetworkAddresses() {
			return addressManager.All ();
		}

		public bool Add(IPEndPoint IPEndPoint) {
			bool added = addressManager.Add (new NetworkAddress (IPEndPoint));
			addressManager.Connected (new NetworkAddress (IPEndPoint));
			return added;
		}

		public void Save() {
			addressManager.SavePeerFile (addressManagerFile, network);
//			LogMessageContext.Create ("Saved");
		}

		public NBitcoin.Protocol.AddressManager GetBitcoinAddressManager() {
			return addressManager;
		}

		public String GetAddressesDesc() {
			String returnValue = "";

			foreach (IPEndPoint IPEndPoint in GetNetworkAddresses()) {
				returnValue += "\n" + IPEndPoint;
			}

			return returnValue;
		}

		public int Count() {
			int i = 0;
			foreach (IPEndPoint IPEndPoint in GetNetworkAddresses()) {
				i++;
			}
			return i;
		}
	}
}

