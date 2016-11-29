using System;
using MsgPack;

namespace NBitcoin.Protocol
{
	public class VerAckPayload
	{
		//TODO: to be removed
		public int MockPropertyZZZ { get; set; } //TODO: cannot use MsgPack on a type with no properties

		public override string ToString()
		{
			return "VerAck";
		}

		//TODO: to be removed
		public VerAckPayload()
		{
			MockPropertyZZZ = 111;
		}
	}
}
