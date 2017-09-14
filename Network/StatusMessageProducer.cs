using System;
using System.Collections.Concurrent;

namespace Network
{
	public interface IStatusMessage { }

	public abstract class StatusMessage<T> : IStatusMessage
	{
		public T Value { get; set; }
	}

	public abstract class BlockChainBlockNumberMessage : StatusMessage<uint>
	{
	}

	public class BlockChainSyncMessage : BlockChainBlockNumberMessage
	{
	}

	public class BlockChainAcceptedMessage : BlockChainBlockNumberMessage
	{
	}

	public enum OutboundStatusEnum
	{
		Disabled,
		Initializing,
		HasValidAddress,
		HasInvalidAddress,
		Accepting
	}

	public class NodeUpnpStatusMessage : StatusMessage<OutboundStatusEnum>
	{
	}

	public class NodeConnectionInfoStatusMessage : StatusMessage<Tuple<int, int>>
	{
	}

	public class StatusMessageProducer : Infrastructure.MessageProducer<IStatusMessage>
	{
        //TODO: refactor
        public static ConcurrentQueue<IStatusMessage> _Queue = new ConcurrentQueue<IStatusMessage>();

		static void Publish(IStatusMessage message)
		{
            if (_Queue != null)
                _Queue.Enqueue(message); 
            
			Instance.PushMessage(message);
		}

		public static OutboundStatusEnum OutboundStatus
		{
			set {
				Publish(new NodeUpnpStatusMessage { Value = value });
			}
		}

		public static Tuple<int, int> Connections
		{
			set
			{
				Publish(new NodeConnectionInfoStatusMessage { Value = value });
			}
		}

		//TODO: naming
		//public static uint LastAccepted 
		//{ 
		//	set 
		//	{ 
  //              Publish(new BlockChainAcceptedMessage { Value = value });
		//	}
		//}

		//TODO: naming
		public static uint LastOrphan
		{
			set
			{
                Publish(new BlockChainSyncMessage { Value = value });
			}
		}
	}
}