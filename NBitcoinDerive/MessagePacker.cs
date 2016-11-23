using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using MsgPack;
using MsgPack.Serialization;
using NBitcoin.Protocol;

namespace NBitcoin
{
	public class CustomIPEndPointSerializer : MessagePackSerializer<IPEndPoint>
	{
		public CustomIPEndPointSerializer(SerializationContext ownerContext) : base(ownerContext) { }

		protected override void PackToCore(Packer packer, IPEndPoint objectTree)
		{
			packer.PackArrayHeader(2);
			packer.Pack(objectTree.Address.ToString());
			packer.Pack(objectTree.Port);
		}

		protected override IPEndPoint UnpackFromCore(Unpacker unpacker)
		{
			if (!unpacker.IsArrayHeader)
			{
				throw new SerializationException("Is not in array header.");
			}

			if (UnpackHelpers.GetItemsCount(unpacker) != 2)
			{
				throw new SerializationException($"Array length count expectation not satisfied, got {UnpackHelpers.GetItemsCount(unpacker)}");
			}

			String ipAddressRaw;
			if (!unpacker.ReadString(out ipAddressRaw))
			{
				throw new SerializationException("Property not found.");
			}
			IPAddress ipAddress = IPAddress.Parse(ipAddressRaw);

			Int32 port;
			if (!unpacker.ReadInt32(out port))
			{
				throw new SerializationException("Property not found.");
			}

			return new IPEndPoint(ipAddress, port);
		}
	}

	public class CustomXxxSerializer : MessagePackSerializer<Consensus.Types.Transaction>
	{
		public CustomXxxSerializer(SerializationContext ownerContext) : base(ownerContext) { }

		protected override void PackToCore(Packer packer, Consensus.Types.Transaction t)
		{
			packer.Pack(Consensus.Serialization.context.GetSerializer<Consensus.Types.Transaction>().PackSingleObject(t));
		}

		protected override Consensus.Types.Transaction UnpackFromCore(Unpacker unpacker)
		{
			return Consensus.Serialization.context.GetSerializer<Consensus.Types.Transaction>().UnpackSingleObject(unpacker.Unpack<byte[]>());
		}
	}

	public class MessagePacker
	{
		private static MessagePacker instance = null;
		public static MessagePacker Instance {
			get
			{
				if (instance == null)
				{
					instance = new MessagePacker();
				}

				return instance;
			}
		}

		private MessagePackSerializer<Message> _MessagePackSerializer = null;
		private MessagePackSerializer<Message> MessagePackSerializer
		{
			get
			{
				if (_MessagePackSerializer == null)
				{
					_MessagePackSerializer = Context.GetSerializer<Message>();
				}

				return _MessagePackSerializer;
			}
		}

		private SerializationContext _Context = null;
		private SerializationContext Context {
			get
			{
				if (_Context == null)
				{
					_Context = Consensus.Serialization.context; // */ new SerializationContext { SerializationMethod = SerializationMethod.Map }; //TODO
					_Context.Serializers.RegisterOverride(new CustomIPEndPointSerializer(_Context));
			//		_Context.Serializers.RegisterOverride(new CustomXxxSerializer(_Context));
				}

				return _Context;
			}
		}

		public Message Unpack(Stream stream)
		{
			return MessagePackSerializer.Unpack(stream);
		}

		public byte[] Pack(Message message)
		{
			MemoryStream memoryStream = new MemoryStream();

			MessagePackSerializer.Pack(memoryStream, message);
			return GetBytes(memoryStream);
		}

		private byte[] GetBytes(MemoryStream stream)
		{
			stream.Position = 0;
			return stream.ToArray();
		}
	}
}
