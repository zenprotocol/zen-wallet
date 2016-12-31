//using System;
//using System.Collections.Concurrent;
//using Consensus;
//using NBitcoin.Protocol;
//using NBitcoin.Protocol.Behaviors;
//using System.Linq;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace NBitcoinDerive
//{
//	public class BroadcastHub<T> : where T 
//	{
//		public static BroadcastHub GetBroadcastHub(Node node)
//		{
//			return GetBroadcastHub(node.Behaviors);
//		}
//		public static BroadcastHub GetBroadcastHub(NodeConnectionParameters parameters)
//		{
//			return GetBroadcastHub(parameters.TemplateBehaviors);
//		}
//		public static BroadcastHub GetBroadcastHub(NodeBehaviorsCollection behaviors)
//		{
//			return behaviors.OfType<BroadcastHubBehavior>().Select(c => c.BroadcastHub).FirstOrDefault();
//		}

//		internal ConcurrentDictionary<byte[], T> BroadcastedTransaction = new ConcurrentDictionary<byte[], T>();
//		internal ConcurrentDictionary<Node, Node> Nodes = new ConcurrentDictionary<Node, Node>();
//		public event TransactionBroadcastedDelegate ransactionBroadcasted;
//		public event TransactionRejectedDelegate TransactionRejected;

//		public IEnumerable<T> BroadcastingTransactions
//		{
//			get
//			{
//				return BroadcastedTransaction.Values;
//			}
//		}

//		internal void OnBroadcastTransaction(T t)
//		{
//			var nodes = Nodes
//						.Select(n => n.Key.Behaviors.Find<BroadcastHubBehavior>())
//						.Where(n => n != null)
//						.ToArray();
//			foreach (var node in nodes)
//			{
//				node.BroadcastTransactionCore(t);
//			}
//		}

//		internal void OnTransactionRejected(T tx, RejectPayload reject)
//		{
//			var evt = TransactionRejected;
//			if (evt != null)
//				evt(tx, reject);
//		}

//		internal void OnTransactionBroadcasted(T tx)
//		{
//			var evt = TransactionBroadcasted;
//			if (evt != null)
//				evt(tx);
//		}

//		//demo
//		private byte[] GetHash(T t)
//		{
//			try
//			{
//				if (t is Types.Transaction)
//					return Merkle.transactionHasher.Invoke(t as Types.Transaction);
//				else if (t is Types.Block)
//					return Merkle.blockHeaderHasher.Invoke(t as Types.BlockHeader);
//				else throw new Exception();
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.Message);
//				return null;
//			}
//		}

//		/// <summary>
//		/// Broadcast a transaction on the hub
//		/// </summary>
//		/// <param name="transaction">The transaction to broadcast</param>
//		/// <returns>The cause of the rejection or null</returns>
//		public Task<RejectPayload> BroadcastTransactionAsync(T transaction)
//		{
//			if (transaction == null)
//				throw new ArgumentNullException("transaction");

//			TaskCompletionSource<RejectPayload> completion = new TaskCompletionSource<RejectPayload>();
//			var hash = GetHash(transaction);
//			if (BroadcastedTransaction.TryAdd(hash, transaction))
//			{
//				TransactionBroadcastedDelegate broadcasted = null;
//				TransactionRejectedDelegate rejected = null;
//				broadcasted = (t) =>
//				{
//					if (GetHash(t) == hash)
//					{
//						completion.SetResult(null);
//						TransactionRejected -= rejected;
//						TransactionBroadcasted -= broadcasted;
//					}
//				};
//				TransactionBroadcasted += broadcasted;
//				rejected = (t, r) =>
//				{
//					if (r.Hash == hash)
//					{
//						completion.SetResult(r);
//						TransactionRejected -= rejected;
//						TransactionBroadcasted -= broadcasted;
//					}
//				};
//				TransactionRejected += rejected;
//				OnBroadcastTransaction(transaction);
//			}
//			return completion.Task;
//		}

//		public BroadcastHubBehavior CreateBehavior()
//		{
//			return new BroadcastHubBehavior(this);
//		}
//	}
//}
