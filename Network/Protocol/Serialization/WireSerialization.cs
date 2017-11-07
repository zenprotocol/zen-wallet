using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using MsgPack;
using MsgPack.Serialization;
using System.Linq;
using Infrastructure;
using NBitcoin.Protocol;

namespace Network.Serialization
{
    //TODO: add version
    public class WireSerialization : Singleton<WireSerialization>
    {
        public uint Magic { get; set; }

		private Dictionary<Type, MessagePackSerializer> _ConsensusExtSerializers;
		private Dictionary<byte, Type> _ConsensusExtTypes;
		private Dictionary<string, Type> _NetworkingPayloadTypes;
		private Dictionary<Type, string> _NetworkingPayloadCodes;
		private List<Type> _EmptyNetworkingPayloadTypes;

		public  WireSerialization()
		{
			IPEndPointSerializer.Register();

			_ConsensusExtSerializers = new Dictionary<Type, MessagePackSerializer>();
			_ConsensusExtTypes = new Dictionary<byte, Type>();
			_NetworkingPayloadTypes = new Dictionary<string, Type>();
			_EmptyNetworkingPayloadTypes = new List<Type>();
			_NetworkingPayloadCodes = new Dictionary<Type, string>();

			var mapping = Consensus.Serialization.context.ExtTypeCodeMapping;
			foreach (var item in mapping)
			{
				Type type;
				switch (item.Key)
				{
					case "Transaction":
						type = typeof(Consensus.Types.Transaction);
						_ConsensusExtSerializers[type] = Consensus.Serialization.context.GetSerializer<Consensus.Types.Transaction>();
						_ConsensusExtTypes[mapping[item.Key]] = type;
						break;
					case "Block":
						type = typeof(Consensus.Types.Block);
						_ConsensusExtSerializers[type] = Consensus.Serialization.context.GetSerializer<Consensus.Types.Block>();
						_ConsensusExtTypes[mapping[item.Key]] = type;
						break;
				}
			}

			_NetworkingPayloadTypes["addr"] = typeof(AddrPayload);
			_NetworkingPayloadTypes["getaddr"] = typeof(GetAddrPayload);
			_NetworkingPayloadTypes["getdata"] = typeof(GetDataPayload);
			_NetworkingPayloadTypes["inv"] = typeof(InvPayload);
			_NetworkingPayloadTypes["ping"] = typeof(PingPayload);
			_NetworkingPayloadTypes["pong"] = typeof(PongPayload);
			_NetworkingPayloadTypes["reject"] = typeof(RejectPayload);
			_NetworkingPayloadTypes["ver"] = typeof(VersionPayload);
			_NetworkingPayloadTypes["verack"] = typeof(VerAckPayload);
			_NetworkingPayloadTypes["gettip"] = typeof(GetTipPayload);

			_EmptyNetworkingPayloadTypes.Add(typeof(VerAckPayload));
			_EmptyNetworkingPayloadTypes.Add(typeof(GetAddrPayload));
			_EmptyNetworkingPayloadTypes.Add(typeof(GetTipPayload));

			foreach (var item in _NetworkingPayloadTypes)
			{
				_NetworkingPayloadCodes[item.Value] = item.Key;
			}
		}

		public byte[] Pack<T>(T payloadObject)
		{
			MemoryStream stream = new MemoryStream();

			Pack(stream, payloadObject);

			return stream.ToArray();
		}

		public void Pack<T>(Stream stream, T payloadObject)
		{
			Type type = payloadObject.GetType();

			if (_ConsensusExtSerializers.ContainsKey(type))
			{
				PackConsensusPayload<T>(stream, payloadObject);
			}
			else
			{
				PackNetworkingPayload<T>(stream, payloadObject);
			}
		}

		private void PackConsensusPayload<T>(Stream stream, T payloadObject)
		{
			Packer packer = Packer.Create(stream);

			packer.PackArrayHeader(3);
			packer.Pack(Magic);
			packer.Pack(GetChecksum(payloadObject));
			packer.PackRawBody(Consensus.Serialization.context.GetSerializer<T>().PackSingleObject(payloadObject));
		}


		private void PackNetworkingPayload<T>(Stream stream, T payloadObject)
		{
			Packer packer = Packer.Create(stream);

			var isEmptyPayload = _EmptyNetworkingPayloadTypes.Contains(payloadObject.GetType());
			packer.PackArrayHeader(isEmptyPayload ? 3 : 4);
			packer.Pack(Magic);
			packer.Pack(GetChecksum(payloadObject));

			if (!_NetworkingPayloadCodes.ContainsKey(payloadObject.GetType())) {
				throw new Exception("Missing: " + payloadObject.GetType());
			}
			    
			string payloadType = _NetworkingPayloadCodes[payloadObject.GetType()];
			packer.Pack(payloadType);

			if (!isEmptyPayload)
			{
				MessagePackSerializer.Get<T>().PackTo(packer, payloadObject);
			}
		}

		public Object Unpack(Stream stream)
		{
			Object returnValue = null;

			Unpacker unpacker = Unpacker.Create(stream);

			unpacker.Read();
			Assert(unpacker.IsArrayHeader);
			Assert(unpacker.ItemsCount == 3 || unpacker.ItemsCount == 4);

			var mainItemsCount = unpacker.ItemsCount;

			Assert(unpacker.Read());
			var magic = unpacker.LastReadData.AsInt32();
			Assert(magic == Magic);

			Assert(unpacker.Read());
			var checksum = unpacker.LastReadData.AsBinary();

			Assert(unpacker.Read());
			if (unpacker.LastReadData.UnderlyingType == typeof(String))
			{
				var payloadTypeCode = unpacker.LastReadData.AsString();

				Type payloadType = _NetworkingPayloadTypes[payloadTypeCode];

				if (_EmptyNetworkingPayloadTypes.Contains(payloadType))
				{
					Assert(mainItemsCount == 3);
					returnValue = Activator.CreateInstance(payloadType);
				}
				else
				{
					Assert(mainItemsCount == 4);
					Assert(unpacker.Read());

					try
					{
						returnValue = MessagePackSerializer.Get(payloadType).UnpackFrom(unpacker);
					}
					catch (Exception e)
					{
						throw new Exception("Error deserializing type " + payloadType.ToString(), e);
					}
				}
			}
			else
			{
				var extObject = unpacker.LastReadData.AsMessagePackExtendedTypeObject();
				Type type = _ConsensusExtTypes[extObject.TypeCode];
				Assert(_ConsensusExtSerializers.ContainsKey(type));
				returnValue = _ConsensusExtSerializers[type].UnpackFrom(unpacker);
			}

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
