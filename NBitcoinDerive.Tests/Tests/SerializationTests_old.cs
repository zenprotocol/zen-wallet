/*using NUnit.Framework;
using System;
using Consensus;
using MsgPack.Serialization;
using MsgPack;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;
using System.Linq;
using Infrastructure;
using System.IO;
using System.Collections.Generic;
using System.Net;
using NBitcoin.Protocol;
using System.Reflection;

namespace NBitcoinDerive.Tests
{
	[TestFixture()]
	public class SerializationTests
	{
		[Test()]
		public void TestCase0()
		{
			//	var stream = new MemoryStream();

			//	var packer = Packer.Create(stream);
			//	//var x = new NBitcoin.Protocol.NetworkAddress();

			//	var p = new XXXPayload() { XXXs = new List<XXX>() { new XXX() { YYY = "YYY!!!!" } } };

			//	packer.Pack(p);

			//	stream.Position = 0;

			//	var unpacker = Unpacker.Create(stream);

			//	new XXXPayload().Unpack(unpacker);
			////	var xsxxx = unpacker.Unpack<XXX>();


			var stream = new MemoryStream();
			IPEndPointSerializer.Register(SerializationContext.Default);
			var wireSerialization = new WireSerialization();

			wireSerialization.Pack(
				stream, 
			    new YYYPayload()
				{
					XXXs = new List<IPEndPoint>() { new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9999) }
				}
			);

			//wireSerialization.Pack(stream, Consensus.Tests.tx);

			stream.Position = 0;

			var data = wireSerialization.Unpack(stream);
		}

		[Test()]
		public void CanSerializeDeserializePingPayload()
		{
			CanSerializeDeserializePayload<PingPayload>(
				(p1, p2) => Assert.That(p1.Nonce, Is.EqualTo(p2.Nonce))
			);
		}

		[Test()]
		public void CanSerializeDeserializeYYYPayload()
		{
			var payload = new YYYPayload();

			var serializer = new PayloadSerializer();
			var data = serializer.PackSingleObject(payload);
			var deserializedPayload = serializer.UnpackSingleObject(data);
		}

		[Test()]
		public void CanSerializeDeserializeAddrPayload()
		{
			//CanSerializeDeserializePayload<AddrPayload>(
			//	(p1, p2) => CollectionAssert.AreEqual(p1.IPEndPoints, p2.IPEndPoints),
			//	p => p.IPEndPoints = new List<IPEndPoint>() { new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9999) }
			//);
		}

		private void CanSerializeDeserializePayload<T>(Action<T, T> assert, Action<T> initialize = null) where T : class, new()
		{
			var payload = new T();

			if (initialize != null)
			{
				initialize(payload);
			}

			var serializer = new PayloadSerializer();
			var data = serializer.PackSingleObject(payload);
			var deserializedPayload = serializer.UnpackSingleObject(data);

			Assert.That(deserializedPayload, Is.TypeOf(typeof(T)));
			assert(deserializedPayload as T, payload);
		}
	}

	public class XXX
	{
		public string YYY { get; set; }
	}

	public class YYYPayload
	{
		public IEnumerable<IPEndPoint> XXXs { get; set; }
	}

	//public class XXXPayload 
	//{
	//	public IEnumerable<XXX> XXXs { get; set; }

	//	public override void Pack(Packer packer)
	//	{
	//		packer.PackArray(XXXs);
	//	}

	//	public override void Unpack(Unpacker unpacker)
	//	{
	//		unpacker.Read();
	//		unpacker.Read();
	//		XXXs = unpacker.Unpack<XXX[]>();
	//	}
	//}

	public class Config : Singleton<Config>
	{
		public byte Version { get; private set; }

		public Config()
		{
			Version = 0x99;
		}
	}

	public class PayloadSerializer : MessagePackSerializer<Object>
	{
		private static readonly Dictionary<String, Type> _PayloadsByCommands;
		private static readonly Dictionary<Type, String> _CommandsPayloads;

		static PayloadSerializer()
		{
			_PayloadsByCommands = new Dictionary<string, Type>();

			_PayloadsByCommands["yyy"] = typeof(YYYPayload);

			_PayloadsByCommands["ping"] = typeof(PingPayload);
			_PayloadsByCommands["addr"] = typeof(AddrPayload);

			_CommandsPayloads = new Dictionary<Type, string>();
			foreach (var item in _PayloadsByCommands)
			{
				_CommandsPayloads[item.Value] = item.Key;
			}
		}

		public PayloadSerializer() : base(SerializationContext.Default)
		{
			SerializationContext.Default.Serializers.RegisterOverride(this);
		}

		protected override void PackToCore(Packer packer, Object objectTree)
		{
			packer.PackArrayHeader(3); //todo: why bother send the header...?
			packer.Pack(_CommandsPayloads[objectTree.GetType()]);
			packer.Pack(Config.Instance.Version);

			packer.Pack(objectTree);
		//	objectTree.Pack(packer);
		}

		protected override Object UnpackFromCore(Unpacker unpacker)
		{
			Assert(unpacker.IsArrayHeader);
			Assert(unpacker.ItemsCount == 3);

			Assert(unpacker.Read());
			var command = unpacker.LastReadData.AsString();
			Assert(!String.IsNullOrEmpty(command));

			Assert(unpacker.Read());
			var version = unpacker.LastReadData.AsByte();
			Assert(version == Config.Instance.Version);

			//var payload = Activator.CreateInstance(_PayloadsByCommands[command]) as Payload;


	//		MethodInfo genericMethod = typeof(Unpacker).GetMethod("Unpack").MakeGenericMethod(_PayloadsByCommands[command]);
	//		var payload = genericMethod.Invoke(null, null); // No target, no arguments
			Assert(unpacker.Read());
			var payload = unpacker.Unpack<YYYPayload>();

		//	Assert(unpacker.Read());
		//	payload.Unpack(unpacker);

			return null;//payload;
		}

		private void Assert(bool assertion, string message = null)
		{
			if (!assertion)
				throw message == null ? new SerializationException() : new Exception(message);
		}
	}

	class IPEndPointSerializer : MessagePackSerializer<IPEndPoint>
	{
		public static void Register(SerializationContext context)
		{
			context.Serializers.RegisterOverride(new IPEndPointSerializer(context));
		}

		private IPEndPointSerializer(SerializationContext context) : base(context)
		{
		}

		protected override void PackToCore(Packer packer, IPEndPoint objectTree)
		{
			packer.Pack(new Tuple<Byte[], int>(objectTree.Address.GetAddressBytes(), objectTree.Port));
		}

		protected override IPEndPoint UnpackFromCore(Unpacker unpacker)
		{
			var tuple = unpacker.Unpack<Tuple<Byte[], int>>();
			return new IPEndPoint(new IPAddress(tuple.Item1), tuple.Item2);
		}
	}

	class WireSerialization
	{
		private const int _Magic = 1;
		private Dictionary<Type, MessagePackSerializer> consensusExtSerializers;
		private Dictionary<byte, Type> consensusExtTypes;

		public WireSerialization()
		{
			consensusExtSerializers = new Dictionary<Type, MessagePackSerializer>();
			consensusExtTypes = new Dictionary<byte, Type>();

			var mapping = Consensus.Serialization.context.ExtTypeCodeMapping;
			foreach (var item in mapping)
			{
				Type type;
				switch (item.Key)
				{
					case "Transaction":
						type = typeof(Types.Transaction);
						consensusExtSerializers[type] = Consensus.Serialization.context.GetSerializer<Types.Transaction>();
						consensusExtTypes[mapping[item.Key]] = type;
						break;
				}
			}
		}

		//stream - as member / parameter??
		public void Pack<T>(Stream stream, T payloadObject)
		{
			Packer packer = Packer.Create(stream);

			packer.PackArrayHeader(3);
			packer.Pack(_Magic);
			packer.Pack(GetChecksum(payloadObject));

			Type type = payloadObject.GetType();
			if (consensusExtSerializers.ContainsKey(type))
			{
				packer.PackRawBody(Consensus.Serialization.context.GetSerializer<T>().PackSingleObject(payloadObject));
			}
			else
			{
				packer.PackArrayHeader(2);
				packer.Pack(COMMAND_TYPE_CODE_GETADDR);
				packer.Pack(payloadObject);
			}
		}

		private const byte COMMAND_TYPE_CODE_GETADDR = 0x01;

		//don't want any reflection, considered to be slow
		private Func<object> GetUnpacker(byte commandTypeCode, Unpacker unpacker)
		{
			switch (commandTypeCode)
			{
				case COMMAND_TYPE_CODE_GETADDR:
					return unpacker.Unpack<YYYPayload>;
			}

			throw new SerializationException();
		}

		private byte GetCommandTypeCode(Object networkProtocolMessage)
		{
			if (networkProtocolMessage is YYYPayload)
			{
				return COMMAND_TYPE_CODE_GETADDR;
			}

			throw new SerializationException();
		}

		public Object Unpack(Stream stream)
		{
			Object returnValue = null;

			Unpacker unpacker = Unpacker.Create(stream);

			Assert(unpacker.Read());
			Assert(unpacker.IsArrayHeader);
		//	Unpacker subTreeUnpacker = unpacker.ReadSubtree(); - checkout readsubtree
			Assert(unpacker.ItemsCount == 3);

			Assert(unpacker.Read());
			var magic = unpacker.LastReadData.AsInt32();
			Assert(magic == _Magic, "Magic mismatch");

			Assert(unpacker.Read());
			var checksum = unpacker.LastReadData.AsBinary();

			Assert(unpacker.Read());
			if (unpacker.IsArrayHeader)
			{
				Assert(unpacker.ItemsCount == 2);
				Assert(unpacker.Read());
				var commandTypeCode = unpacker.LastReadData.AsByte();
				var payloadUnpacker = GetUnpacker(commandTypeCode, unpacker);
				Assert(unpacker.Read());
				returnValue = payloadUnpacker();
			}
			else
			{
				var data = unpacker.LastReadData.AsMessagePackExtendedTypeObject();
				Type type = consensusExtTypes[data.TypeCode];

				if (consensusExtSerializers.ContainsKey(type))
				{
					returnValue = consensusExtSerializers[type].UnpackFrom(unpacker);
				}
			}

			Assert(returnValue != null);
			Assert(checksum.SequenceEqual(GetChecksum(returnValue)), "Checksum mismatch");

			return returnValue;
		}

		private byte[] GetChecksum(Object payloadObject)
		{
			return new byte[] { 0x01, 0x02 };
		}

		private void Assert(bool assertion, string message = null)
		{
			if (!assertion)
				throw message == null ? new SerializationException() : new Exception(message);
		}
	}

}
*/