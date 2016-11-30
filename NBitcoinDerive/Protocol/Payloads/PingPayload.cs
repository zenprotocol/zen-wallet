using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MsgPack;

namespace NBitcoin.Protocol
{
	public class PingPayload
	{
		public string xxx { get; set; }
		public PingPayload()
		{
			_Nonce = RandomUtils.GetUInt64();
		}
		private ulong _Nonce;
		public ulong Nonce
		{
			get
			{
				return _Nonce;
			}
			set
			{
				_Nonce = value;
			}
		}

		public PongPayload CreatePong()
		{
			return new PongPayload()
			{
				Nonce = Nonce
			};
		}

		public override string ToString()
		{
			return "Ping : " + Nonce;
		}
	}
}
