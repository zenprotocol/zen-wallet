#if !NOSOCKET
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin;
using System.Threading;
using Consensus;
using Network;

namespace NBitcoin.Protocol.Behaviors
{
	public delegate void BlockBroadcastedDelegate(Types.Block block);
	public delegate void BlockRejectedDelegate(Types.Block block, RejectPayload reject);
	public class BlockBroadcast
	{
		public BroadcastState State
		{
			get;
			internal set;
		}
		public Types.Block Block
		{
			get;
			internal set;
		}
		internal ulong PingValue
		{
			get;
			set;
		}
		public DateTime AnnouncedTime
		{
			get;
			internal set;
		}
	}

	public class BlockBroadcastHub
	{
		public static BlockBroadcastHub GetBlockBroadcastHub(Node node)
		{
			return GetBlockBroadcastHub(node.Behaviors);
		}
		public static BlockBroadcastHub GetBlockBroadcastHub(NodeConnectionParameters parameters)
		{
			return GetBlockBroadcastHub(parameters.TemplateBehaviors);
		}
		public static BlockBroadcastHub GetBlockBroadcastHub(NodeBehaviorsCollection behaviors)
		{
			return behaviors.OfType<MinerBehavior>().Select(c => c.BlockBroadcastHub).FirstOrDefault();
		}

		internal ConcurrentDictionary<byte[], Types.Block> BroadcastedBlock = new ConcurrentDictionary<byte[], Types.Block>();
		internal ConcurrentDictionary<Node, Node> Nodes = new ConcurrentDictionary<Node, Node>();
		public event BlockBroadcastedDelegate BlockBroadcasted;
		public event BlockRejectedDelegate BlockRejected;

		public IEnumerable<Types.Block> BroadcastingBlocks
		{
			get
			{
				return BroadcastedBlock.Values;
			}
		}

		internal void OnBroadcastBlock(Types.Block block)
		{
			var nodes = Nodes
						.Select(n => n.Key.Behaviors.Find<MinerBehavior>())
						.Where(n => n != null)
						.ToArray();
			foreach (var node in nodes)
			{
				node.BroadcastBlockCore(block);
			}
		}

		internal void OnBlockRejected(Types.Block tx, RejectPayload reject)
		{
			var evt = BlockRejected;
			if (evt != null)
				evt(tx, reject);
		}

		internal void OnBlockBroadcasted(Types.Block tx)
		{
			var evt = BlockBroadcasted;
			if (evt != null)
				evt(tx);
		}

		//demo
		private byte[] GetHash(Types.Block block)
		{
			try
			{
				//	if (block.Block != null)
				return Merkle.blockHeaderHasher.Invoke(block.header);
				//	else
				//		return Merkle.blockHasher.Invoke(block.Block);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				return null;
			}
		}

		/// <summary>
		/// Broadcast a block on the hub
		/// </summary>
		/// <param name="block">The block to broadcast</param>
		/// <returns>The cause of the rejection or null</returns>
		public Task<RejectPayload> BroadcastBlockAsync(Types.Block block)
		{
			if (block == null)
				throw new ArgumentNullException("block");

			TaskCompletionSource<RejectPayload> completion = new TaskCompletionSource<RejectPayload>();
			var hash = GetHash(block);
			if (BroadcastedBlock.TryAdd(hash, block))
			{
				BlockBroadcastedDelegate broadcasted = null;
				BlockRejectedDelegate rejected = null;
				broadcasted = (t) =>
				{
					if (GetHash(t) == hash)
					{
						completion.SetResult(null);
						BlockRejected -= rejected;
						BlockBroadcasted -= broadcasted;
					}
				};
				BlockBroadcasted += broadcasted;
				rejected = (t, r) =>
				{
					if (r.Hash == hash)
					{
						completion.SetResult(r);
						BlockRejected -= rejected;
						BlockBroadcasted -= broadcasted;
					}
				};
				BlockRejected += rejected;
				OnBroadcastBlock(block);
			}
			return completion.Task;
		}

		//public MinerBehavior CreateBehavior()
		//{
		//	return new MinerBehavior(this);
		//}
	}

	public class MinerBehavior : NodeBehavior
	{
		ConcurrentDictionary<byte[], BlockBroadcast> _HashToBlock = new ConcurrentDictionary<byte[], BlockBroadcast>(new ByteArrayComparer());
		ConcurrentDictionary<ulong, BlockBroadcast> _PingToBlock = new ConcurrentDictionary<ulong, BlockBroadcast>();

		public MinerBehavior() : this(new BlockBroadcastHub())
		{
		}

		MinerBehavior(BlockBroadcastHub blockBroadcastHub) 
		{
			_BlockBroadcastHub = blockBroadcastHub;

			foreach (var tx in _BlockBroadcastHub.BroadcastedBlock)
			{
				_HashToBlock.TryAdd(tx.Key, new BlockBroadcast()
				{
					State = BroadcastState.NotSent,
					Block = tx.Value
				});
			}

//			Miner.MessageProducer.AddMessageListener (new Infrastructure.MessageListener<Miner.NewMinedBlockMessage> (Message => {
//				_BlockBroadcastHub.BroadcastBlockAsync (Message.Block);
//			}));
		}

        public void BroadcastBlock(Types.Block bk)
        {
            _BlockBroadcastHub.BroadcastBlockAsync(bk);
        }

		private readonly BlockBroadcastHub _BlockBroadcastHub;
		public BlockBroadcastHub BlockBroadcastHub
		{
			get
			{
				return _BlockBroadcastHub;
			}
		}

		BlockBroadcast GetBlock(byte[] hash, bool remove)
		{
			BlockBroadcast result;

			if (remove)
			{
				if (_HashToBlock.TryRemove(hash, out result))
				{
					BlockBroadcast unused;
					_PingToBlock.TryRemove(result.PingValue, out unused);
				}
			}
			else
			{
				_HashToBlock.TryGetValue(hash, out result);
			}
			return result;
		}
		BlockBroadcast GetBlock(ulong pingValue, bool remove)
		{
			BlockBroadcast result;

			if (remove)
			{
				if (_PingToBlock.TryRemove(pingValue, out result))
				{
					BlockBroadcast unused;
					_HashToBlock.TryRemove(GetHash(result.Block), out unused);
				}
			}
			else
			{
				_PingToBlock.TryGetValue(pingValue, out result);
			}
			return result;
		}

		void AttachedNode_StateChanged(Node node, NodeState oldState)
		{
			if (node.State == NodeState.HandShaked)
			{
				_BlockBroadcastHub.Nodes.TryAdd(node, node);
			}
		}

		private void AnnounceAll()
		{
			foreach (var broadcasted in _HashToBlock)
			{
				if (broadcasted.Value.State == BroadcastState.NotSent ||
				   (DateTime.UtcNow - broadcasted.Value.AnnouncedTime) < TimeSpan.FromMinutes(5.0))
					Announce(broadcasted.Value, broadcasted.Key);
			}
		}


		internal void BroadcastBlockCore(Types.Block block)
		{
			if (block == null)
				throw new ArgumentNullException("block");
			var tx = new BlockBroadcast();
			tx.Block = block;
			tx.State = BroadcastState.NotSent;
			var hash = GetHash(block);
			if (_HashToBlock.TryAdd(hash, tx))
			{
				Announce(tx, hash);
			}
		}

		private void Announce(BlockBroadcast tx, byte[] hash)
		{
			var node = AttachedNode;
			if (node != null && node.State == NodeState.HandShaked)
			{
				tx.State = BroadcastState.Announced;
				tx.AnnouncedTime = DateTime.UtcNow;
				node.SendMessageAsync(tx.Block).ConfigureAwait(false);
			}
		}

		Timer _Flush;
		protected override void AttachCore()
		{
            AttachedNode.StateChanged += AttachedNode_StateChanged;
			AttachedNode.MessageReceived += AttachedNode_MessageReceived;
			_Flush = new Timer(o =>
			{
				AnnounceAll();
			}, null, 0, (int)TimeSpan.FromMinutes(10).TotalMilliseconds);
		}

		protected override void DetachCore()
		{
            AttachedNode.StateChanged -= AttachedNode_StateChanged;
			AttachedNode.MessageReceived -= AttachedNode_MessageReceived;

			Node unused;
			_BlockBroadcastHub.Nodes.TryRemove(AttachedNode, out unused);
			_Flush.Dispose();
		}

		void AttachedNode_MessageReceived(Node node, IncomingMessage message)
		{
			message.IfPayloadIs<PongPayload>(pong =>
			{
				var tx = GetBlock(pong.Nonce, true);
				if (tx != null)
				{
					tx.State = BroadcastState.Accepted;
					Types.Block unused;
					if (_BlockBroadcastHub.BroadcastedBlock.TryRemove(GetHash(tx.Block), out unused))
					{
						_BlockBroadcastHub.OnBlockBroadcasted(tx.Block);
					}
				}
			});
		}

		public override object Clone()
		{
			return new MinerBehavior(_BlockBroadcastHub);
		}

		public IEnumerable<BlockBroadcast> Broadcasts
		{
			get
			{
				return _HashToBlock.Values;
			}
		}

		//demo
		private byte[] GetHash(Types.Block block)
		{
			return Merkle.blockHeaderHasher.Invoke(block.header);
		}
	}
}
#endif