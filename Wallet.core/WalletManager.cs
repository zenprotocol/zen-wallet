//using System;
//using Infrastructure;
//using NBitcoin.Protocol.Behaviors;
//using Consensus;
//using System.Collections.Generic;
//using Microsoft.FSharp.Collections;
//
//namespace Wallet.core
//{
//	public class WalletManager : ResourceOwner
//	{
//		private readonly BroadcastHubBehavior _BroadcastHubBehavior;
//		private readonly SPVBehavior _SPVBehavior;
//		private readonly BlockChain.BlockChain _BlockChain;
//		private readonly ChainBehavior _ChainBehavior;
//		private BroadcastHub _BroadcastHub;
//		private byte[] genesisBlockHash = new byte[] { 0x01, 0x02 };
//
//		public interface IMessage { }
//		public class TransactionAddToMempoolMessage : IMessage { public Types.Transaction Transaction { get; set; } }
//		public class TransactionAddToStoreMessage : IMessage { public Types.Transaction Transaction { get; set; } }
//
//		private void PushMessage(IMessage message)
//		{
//			MessageProducer<IMessage>.Instance.PushMessage(message);
//		}
//
//		//public delegate Action<Types.Transaction> OnNewTransaction;
//
//		public WalletManager()
//		{
//			_BlockChain = new BlockChain.BlockChain("test", genesisBlockHash);
//			OwnResource (_BlockChain);
//
//			_BlockChain.OnAddedToMempool += transaction => {
//				PushMessage (new TransactionAddToMempoolMessage () { Transaction = transaction });
//			};
//			_BlockChain.OnAddedToStore += transaction =>
//			{
//				PushMessage(new TransactionAddToStoreMessage() { Transaction = transaction });
//			};
//
//			_BroadcastHubBehavior = new BroadcastHubBehavior();
//			_BroadcastHub = _BroadcastHubBehavior.BroadcastHub;
//			_SPVBehavior = new SPVBehavior (_BlockChain, _BroadcastHub);
//			_ChainBehavior = new ChainBehavior(_BlockChain);
//		}
//
//		public void Setup(NodeBehaviorsCollection nodeBehaviorsCollection)
//		{
//			nodeBehaviorsCollection.Add(_BroadcastHubBehavior);
//			nodeBehaviorsCollection.Add(_SPVBehavior);
//			nodeBehaviorsCollection.Add(_ChainBehavior);
//		}
//
//		Random _Random = new Random();
//
//		public void SendTransaction(byte[] address, UInt64 amount)
//		{
//			try
//			{
//				var outputs = new List<Types.Output>();
//
//				var pklock = Types.OutputLock.NewPKLock(address);
//				outputs.Add(new Types.Output(pklock, new Types.Spend(Tests.zhash, amount)));
//
//				var inputs = new List<Types.Outpoint>();
//
//				//	inputs.Add(new Types.Outpoint(address, 0));
//
//				var hashes = new List<byte[]>();
//
//				//hack Concensus into giving a different hash per each tx created
//				var version = (uint)_Random.Next(1000);
//
//				Types.Transaction transaction = new Types.Transaction(version,
//					ListModule.OfSeq(inputs),
//					ListModule.OfSeq(hashes),
//					ListModule.OfSeq(outputs),
//				null);
//
//
//				Consensus.Merkle.transactionHasher.Invoke(transaction);
//
//
//				_BroadcastHub.BroadcastTransactionAsync(transaction);
//
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e);
//			}
//		}
//	}
//}
