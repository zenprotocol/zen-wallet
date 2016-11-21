#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;

namespace NodeCore
{
	//TODO: DEMO CLASS
	public class TransactionBehavior : NodeBehavior
	{
		protected override void AttachCore()
		{
			AttachedNode.MessageReceived += AttachedNode_MessageReceived;
		}

		void AttachedNode_MessageReceived(Node node, IncomingMessage message)
		{
			var transactionPayload = message.Message.Payload as TransactionPayload;
			if(transactionPayload != null)
			{
			}
		}

		protected override void DetachCore()
		{

		}

		#region ICloneable Members

		public override object Clone()
		{
			return new TransactionBehavior();
		}

		#endregion
	}
}
#endif