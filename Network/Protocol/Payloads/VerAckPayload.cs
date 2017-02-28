using System;
using MsgPack;
using MsgPack.Serialization;

namespace NBitcoin.Protocol
{
	public class VerAckPayload
	{
		public override string ToString()
		{
			return "VerAck";
		}
	}
}
