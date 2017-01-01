using System;
using System.Net;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using NBitcoin;
using Infrastructure;
using NBitcoinDerive;

namespace NodeTester
{
	public class RemoteServerTest
	{
		public String Start(IPEndPoint IPEndPoint)
		{
		//	Network network = TestNetwork.Instance;

			NodeConnectionParameters nodeConnectionParameters = new NodeConnectionParameters ();

			Node node = Node.Connect (JsonLoader<Network>.Instance.Value, IPEndPoint); 

			node.VersionHandshake();

			return node.IsConnected == true ? "Success" : "Failure";
		}
	}
}

