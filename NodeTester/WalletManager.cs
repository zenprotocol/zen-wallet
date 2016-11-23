using System;
using Infrastructure;
using NBitcoin.Protocol.Behaviors;
using Consensus;

namespace NodeTester
{
	public class WalletManager : Singleton<WalletManager>
	{
		private readonly BroadcastHubBehavior _BroadcastHubBehavior;
		private readonly SPVBehavior _SPVBehavior;
		private readonly BlockChain.BlockChain _BlockChain;

		public interface IMessage { }
		public class TransactionReceivedMessage : IMessage { public Types.Transaction Transaction { get; set; } }

		LogMessageContext LogMessageContext = new LogMessageContext("Wallet");

		private void PushMessage(IMessage message)
		{
			MessageProducer<IMessage>.Instance.PushMessage(message);
		}

		public WalletManager()
		{
			_BlockChain = new BlockChain.BlockChain("test");
			_BlockChain.OnAddedToMempool += (obj) =>
			{
			};
			_BroadcastHubBehavior = new BroadcastHubBehavior();
			_SPVBehavior = new SPVBehavior(transaction => {
				LogMessageContext.Create("TransactionReceived");
				PushMessage(new TransactionReceivedMessage() { Transaction = transaction });
				_BlockChain.HandleNewTransaction(transaction);
			});
		}

		public void Setup(NodeBehaviorsCollection nodeBehaviorsCollection)
		{
			nodeBehaviorsCollection.Add(_BroadcastHubBehavior);
			nodeBehaviorsCollection.Add(_SPVBehavior);
		}
	}
}
