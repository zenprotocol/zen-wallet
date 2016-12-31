using System.Threading;
using Consensus;
using System.Linq;
using System;

namespace NBitcoin.Protocol.Behaviors
{
	public class ChainBehavior : NodeBehavior
	{
		private Timer _Refresh;
		private BlockChain.BlockChain _BlockChain;
		private bool IsTipOld;

		public ChainBehavior(BlockChain.BlockChain blockChain)
		{
			_BlockChain = blockChain;
			IsTipOld = _BlockChain.IsTipOld;
		}

		protected override void AttachCore()
		{
			AttachedNode.StateChanged += AttachedNode_StateChanged;
			AttachedNode.MessageReceived += AttachedNode_MessageReceived;
		}

		protected override void DetachCore()
		{
			AttachedNode.StateChanged -= AttachedNode_StateChanged;
			AttachedNode.MessageReceived -= AttachedNode_MessageReceived;
		}


		void AttachedNode_StateChanged(Node node, NodeState oldState)
		{
			//TODO: to check: don't send to self

			if (node.State == NodeState.HandShaked && IsTipOld)
			{
				AttachedNode.SendMessageAsync(new GetTipPayload());
			}
		}


		void AttachedNode_MessageReceived(Node node, IncomingMessage message)
		{
			message.IfPayloadIs<Types.Block>(bk =>
			{
				var result = _BlockChain.HandleNewBlock(bk);

				switch (result)
				{
					case BlockChain.BlockChainAddBlockOperation.Result.AddedOrphan:
						node.SendMessageAsync(new GetDataPayload(new InventoryVector[] {
							new InventoryVector(InventoryType.MSG_BLOCK, bk.header.parent)
						}));
						break;
					case BlockChain.BlockChainAddBlockOperation.Result.Rejected:
						node.SendMessageAsync(new RejectPayload()
						{
							Hash = Consensus.Merkle.blockHeaderHasher.Invoke(bk.header),
							Code = RejectCode.INVALID,
							Message = "bk"
						});
						break;
				}
			});

			message.IfPayloadIs<GetTipPayload>(getTip =>
			{
				var tip = _BlockChain.Tip;

				if (tip != null)
				{
					NodeServerTrace.Information("Sending tip: " + BitConverter.ToString(Merkle.blockHeaderHasher.Invoke(tip.Value.header)));
					node.SendMessageAsync(tip.Value);
				}
				else 
				{
					NodeServerTrace.Information("No tip to send");
				}
			});

			message.IfPayloadIs<GetDataPayload>(getData =>
			{
				foreach (var inventory in getData.Inventory.Where(i => i.Type == InventoryType.MSG_BLOCK))
				{
					var bk = _BlockChain.GetBlock(inventory.Hash);

					if (bk != null)
					{
						NodeServerTrace.Information("Sending block: " + BitConverter.ToString(Merkle.blockHeaderHasher.Invoke(bk.header)));
						node.SendMessageAsync(bk);
					}
				}
			});
		}

		#region ICloneable Members

		public override object Clone()
		{
			var behavior = new ChainBehavior(_BlockChain);
			return behavior;
		}

		#endregion
	}
}