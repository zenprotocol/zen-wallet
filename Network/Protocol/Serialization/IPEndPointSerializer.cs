using System;
using System.Net;
using MsgPack;
using MsgPack.Serialization;
using NBitcoin.Protocol;

namespace Network.Serialization
{
	public class IPEndPointSerializer : MessagePackSerializer<IPEndPoint>
	{
		public static IPEndPointSerializer Register()
		{
			return Register(SerializationContext.Default);
		}

		public static IPEndPointSerializer Register(SerializationContext context)
		{
			var serializer = new IPEndPointSerializer(context);
			context.Serializers.RegisterOverride(serializer);

			return serializer;
		}

		private IPEndPointSerializer(SerializationContext context) : base(context)
		{
		}

		protected override void PackToCore(Packer packer, IPEndPoint objectTree)
		{
		//	packer.Pack(objectTree.Address.GetAddressBytes());
		//	packer.Pack(objectTree.Port);
			packer.Pack(new Tuple<Byte[], int>(objectTree.Address.GetAddressBytes(), objectTree.Port));
		}

		protected override IPEndPoint UnpackFromCore(Unpacker unpacker)
		{
			var tuple = unpacker.Unpack<Tuple<Byte[], int>>();
			return new IPEndPoint(new IPAddress(tuple.Item1), tuple.Item2);
		
			//var addressBytes = unpacker.LastReadData.AsBinary();
			//unpacker.Read();
			//var port = unpacker.LastReadData.AsInt32();
			//unpacker.Read();

			//return new IPEndPoint(new IPAddress(addressBytes), port);
		}
	}
}
