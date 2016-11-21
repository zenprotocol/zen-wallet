using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using NBitcoin;
using Consensus;
using System.Linq;

namespace NBitcoin.Protocol.Behaviors
{
	public class SPVBehavior : NodeBehavior
	{
		Action<Types.Transaction> onNewTransaction;
		//demo
		private byte[] GetHash(Types.Transaction transaction)
		{
			return Merkle.transactionHasher.Invoke(transaction);
		}

		//BlockchainBuilder _Builder;
		public SPVBehavior(/*BlockchainBuilder builder*/Action<Types.Transaction> onNewTransaction)
		{
			this.onNewTransaction = onNewTransaction;
		//	_Builder = builder;
		}
		protected override void AttachCore()
		{
			lock (Nodes)
			{
				Nodes.Add(AttachedNode);
			}
		//	_Builder.NewBlock += _Builder_NewBlock;
		//	_Builder.NewTransaction += _Builder_NewTransaction;
			AttachedNode.MessageReceived += AttachedNode_MessageReceived;
		}

		void AttachedNode_MessageReceived(Node node, IncomingMessage message)
		{
			//var filterload = message.Message.Payload as FilterLoadPayload;
			//if (filterload != null)
			//{
			//	_Filter = filterload.Object;
			//}
			//var filteradd = message.Message.Payload as FilterAddPayload;
			//if (filteradd != null)
			//{
			//	_Filter.Insert(filteradd.Data);
			//}
			//var getdata = message.Message.Payload as GetDataPayload;
			//if (getdata != null)
			//{
			//	foreach (var inv in getdata.Inventory)
			//	{
			//		if (inv.Type == InventoryType.MSG_FILTERED_BLOCK && _Filter != null)
			//		{
			//			var merkle = new MerkleBlock(_Blocks[inv.Hash], _Filter);
			//			AttachedNode.SendMessageAsync(new MerkleBlockPayload(merkle));
			//			foreach (var tx in merkle.PartialMerkleTree.GetMatchedTransactions())
			//			{
			//				if (_Known.TryAdd(tx, tx))
			//				{
			//					AttachedNode.SendMessageAsync(new InvPayload(InventoryType.MSG_TX, tx));
			//				}
			//			}
			//		}
			//		var found = FindTransaction(inv.Hash);
			//		if (inv.Type == InventoryType.MSG_TX && found != null)
			//			AttachedNode.SendMessageAsync(new TxPayload(found));
			//	}
			//}
			//var mempool = message.Message.Payload as MempoolPayload;
			//if (mempool != null)
			//{
			//	foreach (var tx in _Builder.Mempool)
			//	{
			//		BroadcastCore(tx.Value);
			//	}
			//}

			var invs = message.Message.Payload as InvPayload;
			if (invs != null)
			{
				node.SendMessageAsync(new GetDataPayload(invs.ToArray()));
			}

			var txPayload = message.Message.Payload as TxPayload;
			if (txPayload != null)
			{
				//if (!_ReceivedTransactions.TryAdd(GetHash(txPayload.Transaction), txPayload.Transaction))
				//{
				//	node.SendMessageAsync(new RejectPayload()
				//	{
				//		Hash = GetHash(txPayload.Transaction),
				//		Code = RejectCode.DUPLICATE,
				//		Message = "tx"
				//	});
				//}
				//else
				//{
					foreach (var other in Nodes.Where(n => n != node))
					{
						other.SendMessageAsync(new InvPayload(txPayload.Transaction));
					}
				//}
				onNewTransaction(txPayload.Transaction);
			}
		}

		internal List<Node> Nodes = new List<Node>();
		internal ConcurrentDictionary<byte[], Types.Transaction> _ReceivedTransactions = new ConcurrentDictionary<byte[], Types.Transaction>(new ByteArrayComparer());

		//public BloomFilter _Filter;
		//ConcurrentDictionary<byte[], Block> _Blocks = new ConcurrentDictionary<byte[], Block>(new ByteArrayComparer());
		ConcurrentDictionary<byte[], Types.Transaction> _Transactions = new ConcurrentDictionary<byte[], Types.Transaction>(new ByteArrayComparer());
		ConcurrentDictionary<byte[], byte[]> _Known = new ConcurrentDictionary<byte[], byte[]>();
		//void _Builder_NewTransaction(Types.Transaction obj)
		//{
		//	_Transactions.AddOrReplace(obj.GetHash(), obj);
		//	BroadcastCore(obj);
		//}

		//private void BroadcastCore(Types.Transaction obj)
		//{
		//	if (_Builder.Broadcast)
		//		if (_Filter != null && _Filter.IsRelevantAndUpdate(obj) && _Known.TryAdd(obj.GetHash(), obj.GetHash()))
		//		{
		//			AttachedNode.SendMessageAsync(new InvPayload(obj));
		//		}
		//}

		//void _Builder_NewBlock(Block obj)
		//{
		//	_Blocks.AddOrReplace(obj.GetHash(), obj);
		//	foreach (var tx in obj.Transactions)
		//		_Transactions.TryAdd(tx.GetHash(), tx);
		//	if (_Builder.Broadcast)
		//		AttachedNode.SendMessageAsync(new InvPayload(obj));
		//}

		protected override void DetachCore()
		{
		//	_Builder.NewTransaction -= _Builder_NewTransaction;
		//	_Builder.NewBlock -= _Builder_NewBlock;
			AttachedNode.MessageReceived -= AttachedNode_MessageReceived;
		}

		#region ICloneable Members

		public override object Clone()
		{
			var behavior = new SPVBehavior(/*_Builder*/ onNewTransaction);
		//	behavior._Blocks = _Blocks;
			behavior._Transactions = _Transactions;
			behavior._ReceivedTransactions = _ReceivedTransactions;
			behavior.Nodes = Nodes;
			return behavior;
		}

		//Types.Transaction FindTransaction(byte[] id)
		//{
		//	return _Builder.Mempool.TryGet(id) ?? _Transactions.TryGet(id) ?? _ReceivedTransactions.TryGet(id);
		//}

		#endregion
	}
}
