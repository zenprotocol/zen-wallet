using System;
using System.IO;
using System.Net;
using MsgPack.Serialization;
using NBitcoinDerive.Serialization;
using NUnit.Framework;

namespace Compatability.Tests
{
	public class SerializationTests
	{
		[Test()]
		public void CanSerializeDeserializeIPEndpoint()
		{
			IPEndPointSerializer.Register();

			var ipEndPoint = new IPEndPoint(IPAddress.Parse("127.1.1.1"), 7654);

			byte[] data = SerializationContext.Default.GetSerializer<IPEndPoint>().PackSingleObject(ipEndPoint);

			var ipEndPointUnpacked = SerializationContext.Default.GetSerializer<IPEndPoint>().UnpackSingleObject(data);

			Assert.That(ipEndPoint.Port, Is.EqualTo(ipEndPointUnpacked.Port));
			Assert.That(ipEndPoint.Address.ToString(), Is.EqualTo(ipEndPointUnpacked.Address.ToString()));
		}
	}
}
