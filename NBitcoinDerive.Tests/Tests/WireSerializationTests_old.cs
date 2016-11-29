using NUnit.Framework;
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
/*
namespace NBitcoinDerive.Tests
{
	[TestFixture()]
	public class WireSerializationTests
	{
//		[Test()]
//		public void TestCase()
//		{
//			var context = Consensus.Serialization.context; 

//			context.Serializers.RegisterOverride(
//				new WireSerialization.WireMessageSerializer(context)
//			);

//			var serializer = context.GetSerializer<WireSerialization.WireMessage>();

//			var message = new WireSerialization.WireMessage(
//				1, 
//				new byte[] { 0x01, 0x02 }, 
//				new byte[] { 0x03, 0x04 }
//			);

//			var data = serializer.PackSingleObject(message);

//			var unpackedMessage = serializer.UnpackSingleObject(data);

//			Assert.That(message.Magic, Is.EqualTo(unpackedMessage.Magic));
//			Assert.That(message.Checksum.SequenceEqual(unpackedMessage.Checksum));
//			Assert.That(((byte[])message.Data).SequenceEqual((byte[])unpackedMessage.Data));
//		}

//		[Test()]
//		public void TestCase1()
//		{
//			var tx = Consensus.Tests.tx;
////			var data = Merkle.serialize<Types.Transaction>(tx);
//			var bytesData = WireSerialization.Instance.Pack(tx);
//			var stream = new MemoryStream();
//			stream.Write(bytesData, 0, bytesData.Length);
//			stream.Position = 0;

//			////

//			var unpackedData = WireSerialization.Instance.Unpack(stream) as Types.Transaction;

//		//	var deserializedTx = Consensus.Serialization.context.GetSerializer<Consensus.Types.Transaction>().UnpackSingleObject(unpackedData);
	
//		//	Assert.That(deserializedTx is Types.Transaction);
//		}

		[Test()]
		public void TestCase0()
		{
			var stream = new MemoryStream();

			var packer = Packer.Create(stream);
			var x = new XXX() { A = "xxxxx", B = "yyyyyy" };
			packer.Pack(x);
			            
			stream.Position = 0;

			var unpacker = Unpacker.Create(stream);
			var xsxxx = unpacker.Unpack<XXX>();
		}

		public class XXX
		{
			public string A { get; set; }
			public string B { get; set; }
}

		[Test()]
		public void TestCase1()
		{
			var stream = new MemoryStream();
			WireSerialization.Instance.Pack(stream, Consensus.Tests.tx);
			stream.Position = 0;

			var data = WireSerialization.Instance.Unpack(stream);
		}

		private void CanSerializeDeserializePayload<T>(Action<T,T> assert, Action<T> initialize = null) where T : Payload, new()
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

		[Test()]
		public void CanSerializeDeserializePingPayload()
		{
			CanSerializeDeserializePayload<PingPayload>(
				(p1,p2) => Assert.That(p1.Nonce, Is.EqualTo(p2.Nonce))
			);
		}

		[Test()]
		public void CanSerializeDeserializeAddrPayload()
		{
			CanSerializeDeserializePayload<AddrPayload>(
				(p1, p2) => CollectionAssert.AreEqual(p1.IPEndPoints, p2.IPEndPoints),
				p => p.IPEndPoints = new List<IPEndPoint>() { new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9999) }
			);
		}


//		[Test()]
//		public void TestCase3()
//		{
//			var serializer = new Test2Serializer();

//			var data = serializer.PackSingleObject("");


//			serializer.UnpackSingleObject(data);
//		}
	}

	public class Config : Singleton<Config>
	{
		public byte Version { get; private set; }

		public Config()
		{
			Version = 0x99;
		}
	}

	public class PayloadSerializer : MessagePackSerializer<Payload>
	{
		private static readonly Dictionary<String, Type> _PayloadsByCommands;
		private static readonly Dictionary<Type, String> _CommandsPayloads;

		//private PayloadSerializer _Instance = null;

		//public PayloadSerializer Instance
		//{
		//	get
		//	{
		//		if (_Instance == null)
		//		{
		//			_Instance = new PayloadSerializer();
		//		}

		//		return _Instance;
		//	}
		//}

		static PayloadSerializer()
		{
			_PayloadsByCommands = new Dictionary<string, Type>();

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

		protected override void PackToCore(Packer packer, Payload objectTree)
		{
			packer.PackArrayHeader(3); //todo: why bother send the header...?
			packer.Pack(_CommandsPayloads[objectTree.GetType()]);
			packer.Pack(Config.Instance.Version);
			objectTree.PackToMessage(packer, null);
		}

		protected override Payload UnpackFromCore(Unpacker unpacker)
		{
			Assert(unpacker.IsArrayHeader);
			Assert(unpacker.ItemsCount == 3);

			Assert(unpacker.Read());
			var command = unpacker.LastReadData.AsString();
			Assert(!String.IsNullOrEmpty(command));

			Assert(unpacker.Read());
			var version = unpacker.LastReadData.AsByte();
			Assert(version == Config.Instance.Version);

			var payload = Activator.CreateInstance(_PayloadsByCommands[command]) as Payload;

			Assert(unpacker.Read());
			payload.UnpackFromMessage(unpacker);

			return payload;
		}

		private void Assert(bool assertion, string message = null)
		{
			if (!assertion)
				throw message == null ? new SerializationException() : new Exception(message);
		}
	}

	public abstract class Payload : IPackable, IUnpackable
	{
		public abstract void PackToMessage(Packer packer, PackingOptions options);
		public abstract void UnpackFromMessage(Unpacker unpacker);
	}

	class PingPayload : Payload
	{
		public int Nonce { get; private set; }

		public PingPayload()
		{
			Nonce = 1;
		}

		public override void PackToMessage(Packer packer, PackingOptions options)
		{
			packer.Pack(Nonce);
		}

		public override void UnpackFromMessage(Unpacker unpacker)
		{
			unpacker.Unpack<int>();
		}
	}

	class AddrPayload : Payload
	{
		public IEnumerable<IPEndPoint> IPEndPoints { get; set; }

		public override void PackToMessage(Packer packer, PackingOptions options)
		{
			packer.PackArray(IPEndPoints.Select(e => new Tuple<Byte[], int>(e.Address.GetAddressBytes(), e.Port)));
		}

		public override void UnpackFromMessage(Unpacker unpacker)
		{
			if (!unpacker.IsArrayHeader)
			{
				throw new SerializationException();
			}

			IPEndPoints = unpacker.Unpack<Tuple<Byte[], int>[]>().Select(t => new IPEndPoint(new IPAddress(t.Item1), t.Item2));
		}
	}



	//public class TestSerializer : MessagePackSerializer<String>
	//{
	//	private Dictionary<byte, MessagePackSerializer> extensionSerializers = new Dictionary<byte, MessagePackSerializer>();

	//	public TestSerializer() : base(SerializationContext.Default)
	//	{
	//		SerializationContext.Default.Serializers.RegisterOverride(this);

	//		var mapping = Consensus.Serialization.context.ExtTypeCodeMapping;
	//		foreach (var item in mapping)
	//		{
	//			MessagePackSerializer serializer = null;
	//			switch (item.Key)
	//			{
	//				case "Transaction":
	//					serializer = Consensus.Serialization.context.GetSerializer<Types.Transaction>();
	//					break;
	//			}
	//			extensionSerializers[mapping[item.Key]] = serializer;
	//		}
	//	}

	//	protected override void PackToCore(Packer packer, String objectTree)
	//	{

	//	}

	//	protected override String UnpackFromCore(Unpacker unpacker)
	//	{
	//		try
	//		{
	//			var x = unpacker.LastReadData.AsMessagePackExtendedTypeObject();

	//			//		x.TypeCode
	//			//	var y = Consensus.Serialization.context.GetSerializer<
	//			//					 Consensus.Types.Transaction>().UnpackFrom(unpacker);

	//			var y = extensionSerializers[x.TypeCode].UnpackFrom(unpacker);
	//				                 //x.GetBody());
	//			Console.WriteLine("ok");
	//		}
	//		catch (Exception e)
	//		{
	//			Console.WriteLine(e.Message);
	//		}

	//		return "";
	//	}
	//}


	//public class Test2Serializer : MessagePackSerializer<String>
	//{
	//	TestSerializer _TestSerializer = new TestSerializer();
	//	public Test2Serializer() : base(SerializationContext.Default)
	//	{
	//		SerializationContext.Default.Serializers.RegisterOverride(this);
	//	}

	//	protected override void PackToCore(Packer packer, String objectTree)
	//	{
	//		var tx = Consensus.Tests.tx;
	//		var data = Merkle.serialize<Types.Transaction>(tx);
			
	//		//packer.PackExtendedTypeValue(.Pack(data);
	//	}

	//	protected override String UnpackFromCore(Unpacker unpacker)
	//	{
	//		try
	//		{
	//			unpacker.LastReadData.AsMessagePackExtendedTypeObject();
	//			var tt = Unpacking.UnpackExtendedTypeObject(unpacker.LastReadData.AsBinary());

	//			//	_TestSerializer.UnpackSingleObject(unpacker.LastReadData.AsBinary());


	//			//	var x = unpacker.LastReadData.AsMessagePackExtendedTypeObject();

	//			//		x.TypeCode
	//			var y = Consensus.Serialization.context.GetSerializer<
	//							 Consensus.Types.Transaction>().UnpackSingleObject(tt.Value.GetBody());

	//			//var y = extensionSerializers[x.TypeCode].UnpackFrom(unpacker);
	//			//x.GetBody());
	//			Console.WriteLine("ok");
	//		}
	//		catch (Exception e)
	//		{
	//			Console.WriteLine(e.Message);
	//		}

	//		return "";
	//	}
	//}



	class WireSerialization : Singleton<WireSerialization>
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

		//public class WireMessage
		//{
		//	public int Magic { get; private set; }
		//	public byte[] Checksum { get; private set; }
		//	public byte[] Data { get; private set; }
		//	public Type Type { get; private set; }

		//	public WireMessage(int magic, byte[] checksum, byte[] data, Type type)
		//	{
		//		Magic = magic;
		//		Checksum = checksum;
		//		Data = data;
		//		Type = type;
		//	}
		//}

		//stream - as member / parameter??
		public void Pack<T>(Stream stream, T payloadObject)
		{
			Packer packer = Packer.Create(stream);

			packer.PackArrayHeader(3);
			packer.Pack(_Magic);
			packer.Pack(GetChecksum(payloadObject));

			if (payloadObject is Payload)
			{
				var payload = payloadObject as Payload;
				packer.PackArrayHeader(2);
				payload.PackToMessage(packer, null);
				packer.Pack(GetChecksum(payloadObject));
			}
			else 
			{
				Type type = payloadObject.GetType();
				if (consensusExtSerializers.ContainsKey(type))
				{
					packer.PackRawBody(Consensus.Serialization.context.GetSerializer<T>().PackSingleObject(payloadObject));
				}
			}
		}

		public Object Unpack(Stream stream)
		{
			Object returnValue = null;

			Unpacker unpacker = Unpacker.Create(stream);
			Assert(unpacker.Read());

			Assert(unpacker.IsArrayHeader);
			Unpacker subTreeUnpacker = unpacker.ReadSubtree();
			Assert(subTreeUnpacker.ItemsCount == 3);

			Assert(subTreeUnpacker.Read());
			var magic = subTreeUnpacker.LastReadData.AsInt32();

			Assert(subTreeUnpacker.Read());
			Assert(magic == _Magic, "Magic mismatch");

			var checksum = subTreeUnpacker.LastReadData.AsBinary();
			Assert(subTreeUnpacker.Read());

			var data = subTreeUnpacker.LastReadData.AsMessagePackExtendedTypeObject();
			Type type = consensusExtTypes[data.TypeCode];

			if (consensusExtSerializers.ContainsKey(type))
			{
				returnValue = consensusExtSerializers[type].UnpackFrom(unpacker);
				Assert(checksum.SequenceEqual(GetChecksum(returnValue)), "Checksum mismatch");
			}
			else
			{
				// use the payload serializer here......
			}
			// other network serializers

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

		//public class WireMessageSerializer : MessagePackSerializer<WireMessage>
		//{
		//	public WireMessageSerializer() : base(SerializationContext.Default)
		//	{
		//		SerializationContext.Default.Serializers.RegisterOverride(this);
		//	}

		//	//public WireMessageSerializer(SerializationContext context) : base(context) { 
		//	//	context.Serializers.RegisterOverride(this);
		//	//}

		//	protected override void PackToCore(Packer packer, WireMessage objectTree)
		//	{						
		//		packer.PackArrayHeader(3);

		//		packer.Pack(objectTree.Magic);
		//		packer.Pack(objectTree.Checksum);
		//		packer.PackRawBody(objectTree.Data);
		//	}

		//	protected override WireMessage UnpackFromCore(Unpacker unpacker)
		//	{
		//		if (!unpacker.IsArrayHeader || unpacker.ItemsCount != 3)
		//		{
		//			throw new SerializationException();
		//		}

		//		Unpacker subTreeUnpacker = unpacker.ReadSubtree();

		//		if (subTreeUnpacker.ItemsCount != 3)
		//		{
		//			throw new SerializationException();
		//		}

		//		if (!subTreeUnpacker.Read())
		//		{
		//			throw new SerializationException();
		//		}

		//		var magic = subTreeUnpacker.LastReadData.AsInt32();

		//		if (!subTreeUnpacker.Read())
		//		{
		//			throw new SerializationException();
		//		}

		//		var checksum = subTreeUnpacker.LastReadData.AsBinary();

		//		if (!subTreeUnpacker.Read())
		//		{
		//			throw new SerializationException();
		//		}

		//		var data = subTreeUnpacker.LastReadData.AsMessagePackExtendedTypeObject();

		//		////		x.TypeCode
		//		//	var y = Consensus.Serialization.context.GetSerializer<
		//		//					 Consensus.Types.Transaction>().UnpackFrom(unpacker);

			
		//		return new WireMessage(magic, checksum, data, );
		//	}
		//}
	}
}

*/