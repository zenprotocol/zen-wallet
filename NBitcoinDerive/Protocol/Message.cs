using MsgPack.Serialization;

namespace NBitcoin.Protocol
{
	public class Message
	{
		uint magic;

		public uint Magic
		{
			get
			{
				return magic;
			}
			set
			{
				magic = value;
			}
		}

		[MessagePackKnownType("1", typeof(VerAckPayload))]
		[MessagePackKnownType("2", typeof(VersionPayload))]
		[MessagePackKnownType("3", typeof(PingPayload))]
		[MessagePackKnownType("4", typeof(PongPayload))]
		[MessagePackKnownType("5", typeof(AddrPayload))]
		[MessagePackKnownType("6", typeof(GetAddrPayload))]
		[MessagePackKnownType("7", typeof(RejectPayload))]
		[MessagePackKnownType("8", typeof(TransactionPayload))]
		[MessagePackKnownType("9", typeof(InvPayload))]
		[MessagePackKnownType("A", typeof(GetDataPayload))]
		[MessagePackKnownType("B", typeof(TxPayload))]
		public Payload Payload { get; set; }

		public override string ToString()
		{
			return string.Format($"[Message: Payload={Payload}]");
		}
	}
}
