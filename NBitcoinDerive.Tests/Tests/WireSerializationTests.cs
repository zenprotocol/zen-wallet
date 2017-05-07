using NUnit.Framework;
using System;
using NBitcoinDerive.Serialization;
using System.IO;
using NBitcoin.Protocol;
using System.Net;
using System.Threading.Tasks;

namespace NBitcoinDerive.Tests
{
	[TestFixture()]
	public class WireSerializationTests
	{
		[Test()]
		public void CanSerializeDeserializeVersionPayloadd()
		{
			CanSerializeDeserializePayload<VersionPayload>(
				(p1, p2) => {
				}, p => {
					p.Nonce = 1;
					p.UserAgent = "xxx";
					p.Version = ProtocolVersion.BIP0031_VERSION;
					p.Timestamp = DateTimeOffset.UtcNow;
					p.AddressReceiver = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 9999);
					p.AddressFrom = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 9999);
					p.Relay = false;
					p.Services = NodeServices.GetUTXO;
				}
			);
		}

		[Test()]
		public void CanSerializeDeserializePingPayload()
		{
			CanSerializeDeserializePayload<PingPayload>(
				(p1, p2) => Assert.That(p1.Nonce, Is.EqualTo(p2.Nonce)),
				p => p.xxx = "xxxx"
			);
		}

		[Test()]
		public void CanSerializeDeserializeVerAckPayload()
		{
			CanSerializeDeserializePayload<VerAckPayload>();
		}

		private void CanSerializeDeserializePayload<T>(Action<T, T> assert = null, Action<T> initialize = null) where T : class, new()
		{
			var payload = new T();

			if (initialize != null)
			{
				initialize(payload);
			}

			var stream = new MemoryStream();

			WireSerialization.Instance.Pack(stream, payload);
			stream.Position = 0;

			var deserializedPayload = WireSerialization.Instance.Unpack(stream);

			Assert.That(deserializedPayload, Is.TypeOf(typeof(T)));
			if (assert != null)
			{
				assert(deserializedPayload as T, payload);
			}
		}
	}
}
