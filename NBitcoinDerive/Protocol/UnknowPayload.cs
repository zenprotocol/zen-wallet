using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public class UnknowPayload : Payload
	{
		public UnknowPayload()
		{

		}

		private byte[] _Data = new byte[0];
		public byte[] Data
		{
			get
			{
				return _Data;
			}
			set
			{
				_Data = value;
			}
		}
	}
}
