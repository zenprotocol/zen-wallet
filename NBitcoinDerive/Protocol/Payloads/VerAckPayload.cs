using System;

namespace NBitcoin.Protocol
{
	public class VerAckPayload : Payload
	{
		public int MockProperty { get; set; } //TODO: cannot use MsgPack on a type with no properties

		public override string ToString()
		{
			return "VerAck";
		}
	}
}
