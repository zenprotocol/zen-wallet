#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol.Filters
{
	public class ActionFilter : INodeFilter
	{
		Action<IncomingMessage, Action> _OnIncoming;
		Action<Node, Object, Action> _OnSending;
		public ActionFilter(Action<IncomingMessage, Action> onIncoming = null, Action<Node, Object, Action> onSending = null)
		{
			_OnIncoming = onIncoming ?? new Action<IncomingMessage, Action>((m, n) => n());
			_OnSending = onSending ?? new Action<Node, Object, Action>((m, p, n) => n());
		}
		#region INodeFilter Members

		public void OnReceivingMessage(IncomingMessage message, Action next)
		{
			_OnIncoming(message, next);
		}

		public void OnSendingMessage(Node node, Object payload, Action next)
		{
			_OnSending(node, payload, next);
		}

		#endregion
	}
}
#endif