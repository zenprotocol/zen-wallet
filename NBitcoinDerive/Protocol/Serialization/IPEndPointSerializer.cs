using System;
using System.Net;
using MsgPack;
using MsgPack.Serialization;
using NBitcoin.Protocol;

namespace NBitcoinDerive.Serialization
{
	public class IPEndPointSerializer : MessagePackSerializer<IPEndPoint>
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
}
