using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MsgPack;

namespace NBitcoin.Protocol
{
	public class UnknowPayload
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
