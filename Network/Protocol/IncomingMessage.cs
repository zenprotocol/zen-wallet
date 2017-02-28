#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public class IncomingMessage : Message
	{
		public IncomingMessage(Object payload) : base(payload)
		{
		}
		internal Socket Socket
		{
			get;
			set;
		}
		public Node Node
		{
			get;
			set;
		}
		public long Length
		{
			get;
			set;
		}
	}
}
#endif