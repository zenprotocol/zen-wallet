using MsgPack;

namespace NBitcoin.Protocol
{
	public class PongPayload
	{
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

		public override string ToString()
		{
			return "Pong : " + Nonce;
		}
	}
}
