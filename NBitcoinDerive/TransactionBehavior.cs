//#if !NOSOCKET
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net;
//using System.Text;
//using System.Threading.Tasks;
//using NBitcoin.Protocol;
//using NBitcoin.Protocol.Behaviors;

//namespace NBitcoin
//{
//	//TODO: DEMO CLASS
//	public class TransactionBehavior : NodeBehavior
//	{
//		public NodesCollection ConnectedNodes { get; set; }

//		//public static TransactionBehavior GetTransactionBehavior(Node node)
//		//{
//		//	return GetBroadcastHub(node.Behaviors);
//		//}
//		//public static GetTransactionBehavior GetBroadcastHub(NodeConnectionParameters parameters)
//		//{
//		//	return GetTransactionBehavior(parameters.TemplateBehaviors);
//		//}
//		//public static BroadcastHub GetTransactionBehavior(NodeBehaviorsCollection behaviors)
//		//{
//		//	return behaviors.OfType<TransactionBehavior>().Select(c => c.BroadcastHub).FirstOrDefault();
//		//}

//		protected override void AttachCore()
//		{
//			AttachedNode.MessageReceived += AttachedNode_MessageReceived;

//			if (ConnectedNodes == null)
//			{
//				ConnectedNodes = new NodesCollection();
//			}

//			ConnectedNodes.Add(
//		}

//		void AttachedNode_MessageReceived(Node node, IncomingMessage message)
//		{
//			var transactionPayload = message.Message.Payload as TransactionPayload;
//			if(transactionPayload != null)
//			{
//				Broadcast(transactionPayload);
//			}
//		}

//		protected override void DetachCore()
//		{

//		}

//		private void Broadcast(TransactionPayload transactionPayload)
//		{
//			foreach (var node in ConnectedNodes)
//			{
//				node.SendMessage(transactionPayload);
//			}
//		}

//		#region ICloneable Members

//		public override object Clone()
//		{
//			return new TransactionBehavior();
//		}

//		#endregion
//	}
//}
//#endif