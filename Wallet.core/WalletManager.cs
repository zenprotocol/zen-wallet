using System;
using Infrastructure;
using NBitcoin.Protocol.Behaviors;
using Consensus;
using System.Collections.Generic;
using Microsoft.FSharp.Collections;

namespace Wallet.core
{
	public class WalletManager : Singleton<WalletManager>, IDisposable
	{
		private readonly BroadcastHubBehavior _BroadcastHubBehavior;
		private readonly SPVBehavior _SPVBehavior;
		private readonly BlockChain.BlockChain _BlockChain;
		private BroadcastHub _BroadcastHub;
		private byte[] genesisBlockHash = new byte[] { 0x01, 0x02 };

		public interface IMessage { }
		public class TransactionReceivedMessage : IMessage { public Types.Transaction Transaction { get; set; } }

		private void PushMessage(IMessage message)
		{
			MessageProducer<IMessage>.Instance.PushMessage(message);
		}

		public WalletManager()
		{
			_BlockChain = new BlockChain.BlockChain("test", genesisBlockHash);

			_BlockChain.OnAddedToMempool += transaction => {
				PushMessage (new TransactionReceivedMessage () { Transaction = transaction });
				//_BlockChain.HandleNewTransaction (transaction);
			};

			_BroadcastHubBehavior = new BroadcastHubBehavior();
			_BroadcastHub = _BroadcastHubBehavior.BroadcastHub;
			_SPVBehavior = new SPVBehavior (_BlockChain, _BroadcastHub);
		}

		public void Setup(NodeBehaviorsCollection nodeBehaviorsCollection)
		{
			nodeBehaviorsCollection.Add(_BroadcastHubBehavior);
			nodeBehaviorsCollection.Add(_SPVBehavior);
		}

		Random _Random = new Random();

		public void SendTransaction(byte[] address, UInt64 amount)
		{
			try
			{
				var outputs = new List<Types.Output>();

				var pklock = Types.OutputLock.NewPKLock(address);
				outputs.Add(new Types.Output(pklock, new Types.Spend(Tests.zhash, amount)));

				var inputs = new List<Types.Outpoint>();

				//	inputs.Add(new Types.Outpoint(address, 0));

				var hashes = new List<byte[]>();

				//hack Concensus into giving a different hash per each tx created
				var version = (uint)_Random.Next(1000);

				Types.Transaction transaction = new Types.Transaction(version,
					ListModule.OfSeq(inputs),
					ListModule.OfSeq(hashes),
					ListModule.OfSeq(outputs),
				null);


				Consensus.Merkle.transactionHasher.Invoke(transaction);


				_BroadcastHub.BroadcastTransactionAsync(transaction);

			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}

		public void Dispose()
		{
			_BlockChain.Dispose();
		}
	}
}
