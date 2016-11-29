using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using MsgPack;
using MsgPack.Serialization;
using System.Linq;
using Infrastructure;
using NBitcoin.Protocol;

namespace NBitcoinDerive.Serialization
{
	public class WireSerialization : Singleton<WireSerialization>
	{
		private const int _Magic = 1;
		private Dictionary<Type, MessagePackSerializer> consensusExtSerializers;
		private Dictionary<byte, Type> consensusExtTypes;

		//TODO
		private const byte COMMAND_TYPE_CODE_ADDR = 0x01;
		private const byte COMMAND_TYPE_CODE_GETADDR = 0x02;
		private const byte COMMAND_TYPE_CODE_GETDATA = 0x03;
		private const byte COMMAND_TYPE_CODE_INV = 0x04;
		private const byte COMMAND_TYPE_CODE_PING = 0x05;
		private const byte COMMAND_TYPE_CODE_PONG = 0x06;
		private const byte COMMAND_TYPE_CODE_REJECT = 0x07;
		private const byte COMMAND_TYPE_CODE_VERACK = 0x08;
		private const byte COMMAND_TYPE_CODE_VER = 0x09;

		public WireSerialization()
		{
			IPEndPointSerializer.Register(SerializationContext.Default);
		//	SerializationContext.Default.SerializationMethod = SerializationMethod.Map;

			consensusExtSerializers = new Dictionary<Type, MessagePackSerializer>();
			consensusExtTypes = new Dictionary<byte, Type>();

			var mapping = Consensus.Serialization.context.ExtTypeCodeMapping;
			foreach (var item in mapping)
			{
				Type type;
				switch (item.Key)
				{
					case "Transaction":
						type = typeof(Consensus.Types.Transaction);
						consensusExtSerializers[type] = Consensus.Serialization.context.GetSerializer<Consensus.Types.Transaction>();
						consensusExtTypes[mapping[item.Key]] = type;
						break;
				}
			}
		}

		//stream - as member / parameter??
		public void Pack<T>(Stream stream, T payloadObject)
		{
			NodeServerTrace.Information("----- pack start " + payloadObject);

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
				packer.Pack(GetCommandTypeCode(payloadObject));

			//	var serializer = MessagePackSerializer.Get<VerAckPayload>(SerializationContext.Default);
			//	serializer.PackTo(packer);
				packer.Pack(payloadObject);
			}

			NodeServerTrace.Information("----- pack end " + payloadObject);

		}

		public byte[] Pack<T>(T payloadObject)
		{
			MemoryStream stream = new MemoryStream();

			Pack(stream, payloadObject);
			stream.Position = 0;

			return stream.GetBuffer();
		}

		//don't want any reflection, considered to be slow
		private Func<object> GetUnpacker(byte commandTypeCode, Unpacker unpacker)
		{
			switch (commandTypeCode)
			{
				case COMMAND_TYPE_CODE_ADDR:
					return unpacker.Unpack<AddrPayload>;
				case COMMAND_TYPE_CODE_GETADDR:
					return unpacker.Unpack<GetAddrPayload>;
				case COMMAND_TYPE_CODE_GETDATA:
					return unpacker.Unpack<GetDataPayload>;
				case COMMAND_TYPE_CODE_INV:
					return unpacker.Unpack<InvPayload>;
				case COMMAND_TYPE_CODE_PING:
					return unpacker.Unpack<PingPayload>;
				case COMMAND_TYPE_CODE_PONG:
					return unpacker.Unpack<PongPayload>;
				case COMMAND_TYPE_CODE_REJECT:
					return unpacker.Unpack<RejectPayload>;
				case COMMAND_TYPE_CODE_VERACK:
					return unpacker.Unpack<VerAckPayload>;
				case COMMAND_TYPE_CODE_VER:
					return unpacker.Unpack<VersionPayload>;
			}

			throw new SerializationException();
		}

		//private Func<MessagePackSerializer> GetSerializer(byte commandTypeCode, Unpacker unpacker)
		//{
		//	switch (commandTypeCode)
		//	{
		//		case COMMAND_TYPE_CODE_ADDR:
		//			return MessagePackSerializer.Get<AddrPayload>;
		//		case COMMAND_TYPE_CODE_GETADDR:
		//			return MessagePackSerializer.Get<GetAddrPayload>;
		//		case COMMAND_TYPE_CODE_GETDATA:
		//			return MessagePackSerializer.Get<GetDataPayload>;
		//		case COMMAND_TYPE_CODE_INV:
		//			return MessagePackSerializer.Get<InvPayload>;
		//		case COMMAND_TYPE_CODE_PING:
		//			return MessagePackSerializer.Get<PingPayload>;
		//		case COMMAND_TYPE_CODE_PONG:
		//			return MessagePackSerializer.Get<PongPayload>;
		//		case COMMAND_TYPE_CODE_REJECT:
		//			return MessagePackSerializer.Get<RejectPayload>;
		//		case COMMAND_TYPE_CODE_VERACK:
		//			return MessagePackSerializer.Get<VerAckPayload>;
		//		case COMMAND_TYPE_CODE_VER:
		//			return MessagePackSerializer.Get<VersionPayload>;
		//	}

		//	throw new SerializationException();
		//}

		private byte GetCommandTypeCode(Object networkProtocolMessage)
		{
			if (networkProtocolMessage is AddrPayload)
			{
				return COMMAND_TYPE_CODE_ADDR;
			}
			else if (networkProtocolMessage is GetAddrPayload)
			{
				return COMMAND_TYPE_CODE_GETADDR;
			}
			else if (networkProtocolMessage is GetDataPayload)
			{
				return COMMAND_TYPE_CODE_GETDATA;
			}
			else if (networkProtocolMessage is InvPayload)
			{
				return COMMAND_TYPE_CODE_INV;
			}
			else if (networkProtocolMessage is PingPayload)
			{
				return COMMAND_TYPE_CODE_PING;
			}
			else if (networkProtocolMessage is PongPayload)
			{
				return COMMAND_TYPE_CODE_PONG;
			}
			else if (networkProtocolMessage is RejectPayload)
			{
				return COMMAND_TYPE_CODE_REJECT;
			}
			else if (networkProtocolMessage is VerAckPayload)
			{
				return COMMAND_TYPE_CODE_VERACK;
			}
			else if (networkProtocolMessage is VersionPayload)
			{
				return COMMAND_TYPE_CODE_VER;
			}
			else 
			{
				throw new SerializationException();
			}
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
			Assert(magic == _Magic);

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

				NodeServerTrace.Information("----- unpacked " + returnValue);
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
			Assert(checksum.SequenceEqual(GetChecksum(returnValue)));

			return returnValue;
		}

		private byte[] GetChecksum(Object payloadObject)
		{
			return new byte[] { 0x01, 0x02 };
		}

		private void Assert(bool assertion)
		{
			if (!assertion)
				throw new SerializationException();
		}
	}
}
