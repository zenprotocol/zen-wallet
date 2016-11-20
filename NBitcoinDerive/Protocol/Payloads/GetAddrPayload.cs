using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	/// <summary>
	/// Ask for known peer addresses in the network
	/// </summary>
	public class GetAddrPayload : Payload
	{
		public int MockProperty { get; set; } //TODO: cannot use MsgPack on a type with no properties

		public override string ToString()
		{
			return "GetAddr";
		}
	}
}
