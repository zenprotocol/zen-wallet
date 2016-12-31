using System;
using Infrastructure;
using NBitcoin.Protocol.Behaviors;
using Consensus;
using System.Collections.Generic;
using Microsoft.FSharp.Collections;
using Infrastructure.Testing.Blockchain;

namespace NodeTester
{
	public class WalletManager : Singleton<WalletManager>, IDisposable
	{
		private readonly BroadcastHubBehavior _BroadcastHubBehavior;
		private readonly SPVBehavior _SPVBehavior;
		private readonly ChainBehavior _ChainBehavior;
		private readonly BlockChain.BlockChain _BlockChain;
		private BroadcastHub _BroadcastHub;

		public interface IMessage { }
		public class TransactionAddToMempoolMessage : IMessage { public Types.Transaction Transaction { get; set; } }
		public class TransactionAddToStoreMessage : IMessage { public Types.Transaction Transaction { get; set; } }

		LogMessageContext LogMessageContext = new LogMessageContext("Wallet");

		private void PushMessage(IMessage message)
		{
			MessageProducer<IMessage>.Instance.PushMessage(message);
		}

		public WalletManager()
		{
			var p = new TestTransactionPool();

			p.Add("t1", 1);
			p.Add("t2", 0);
			p.Add("t3", 0);
			p.Spend("t2", "t1", 0);

			p.Render();

			var genesisBlock = new TestBlock(p.TakeOut("t1").Value);
		//	var block1 = new TestBlock(p.TakeOut("t2").Value, p.TakeOut("t3").Value);
		//	block1.Parent = genesisBlock;

			genesisBlock.Render();
			//	block1.Render();

			var genesisBlockHash = genesisBlock.Value.Key;


			_BlockChain = new BlockChain.BlockChain("test", genesisBlockHash);

			_BlockChain.OnAddedToMempool += transaction => {
				LogMessageContext.Create ("TransactionReceived (mempool)");
				PushMessage (new TransactionAddToMempoolMessage () { Transaction = transaction });
				//_BlockChain.HandleNewTransaction (transaction);
			};
			_BlockChain.OnAddedToStore += transaction =>
			{
				LogMessageContext.Create("TransactionReceived (store)");
				PushMessage(new TransactionAddToStoreMessage() { Transaction = transaction });
				//_BlockChain.HandleNewTransaction (transaction);
			};

			_BroadcastHubBehavior = new BroadcastHubBehavior();
			_BroadcastHub = _BroadcastHubBehavior.BroadcastHub;
			_SPVBehavior = new SPVBehavior (_BlockChain, _BroadcastHub);
			_ChainBehavior = new ChainBehavior(_BlockChain);
		}

		public void Setup(NodeBehaviorsCollection nodeBehaviorsCollection)
		{
			nodeBehaviorsCollection.Add(_BroadcastHubBehavior);
			nodeBehaviorsCollection.Add(_SPVBehavior);
			nodeBehaviorsCollection.Add(_ChainBehavior);
}

		Random _Random = new Random();

		public void SendTransaction(byte[] address, UInt64 amount)
		{
			var outputs = new List<Types.Output>();

			var pklock = Types.OutputLock.NewPKLock(address);
			outputs.Add(new Types.Output(pklock, new Types.Spend(Tests.zhash, amount)));

			var inputs = new List<Types.Outpoint>();

			//inputs.Add(new Types.Outpoint(address, 0));

			var hashes = new List<byte[]>();

			var version = (uint)_Random.Next(1);

			var transaction = new Types.Transaction(version,
				ListModule.OfSeq(inputs),
				ListModule.OfSeq(hashes),
				ListModule.OfSeq(outputs),
				null);

			_BroadcastHub.BroadcastTransactionAsync(transaction);
		}

		public void Dispose()
		{
			_BlockChain.Dispose();
		}
	}
}