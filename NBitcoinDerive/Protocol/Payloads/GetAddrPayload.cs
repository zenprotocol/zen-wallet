using System;
using MsgPack;

namespace NBitcoin.Protocol
{
	/// <summary>
	/// Ask for known peer addresses in the network
	/// </summary>
	public class GetAddrPayload
	{
		//TODO: to be removed
		public int MockProperty { get; set; } //TODO: cannot use MsgPack on a type with no properties

		public override string ToString()
		{
			return "GetAddr";
		}


		//TODO: to be removed
		public GetAddrPayload()
		{
			MockProperty = 333;
		}

	}
}
