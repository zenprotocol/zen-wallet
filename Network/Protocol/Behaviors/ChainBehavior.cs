using System.Threading;
using Consensus;
using System.Linq;
using System;
using System.Collections.Generic;
using BlockChain.Data;
using Network;

namespace NBitcoin.Protocol.Behaviors
{
	public class ChainBehavior : NodeBehavior
	{
		Timer _Refresh;
		BlockChain.BlockChain _BlockChain;
		bool IsTipOld;
		List<Node> Nodes = new List<Node>();

		public ChainBehavior(BlockChain.BlockChain blockChain)
		{
			_BlockChain = blockChain;
			IsTipOld = _BlockChain.IsTipOld;
		}

		protected override void AttachCore()
		{
			lock (Nodes)
			{
				Nodes.Add(AttachedNode);
			}
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
			message.IfPayloadIs<Types.Block>(async bk =>
			{
                var result = await new HandleBlockAction(bk).Publish();

				switch (result.BkResultEnum)
				{
					case BlockChain.BlockVerificationHelper.BkResultEnum.Accepted:
						foreach (var other in Nodes)
						{
							if (other != AttachedNode && other.State == NodeState.HandShaked)
								other.SendMessageAsync(bk);
						}
						break;
					case BlockChain.BlockVerificationHelper.BkResultEnum.AcceptedOrphan:
						StatusMessageProducer.LastOrphan = bk.header.blockNumber;
						node.SendMessageAsync(new GetDataPayload(new InventoryVector[] {
                            new InventoryVector(InventoryType.MSG_BLOCK, result.MissingParent)
						}));
						break;
					case BlockChain.BlockVerificationHelper.BkResultEnum.Rejected:
                        if (result.MissingParent != null)
                        {
                            node.SendMessageAsync(new GetDataPayload(new InventoryVector[] {
                               new InventoryVector(InventoryType.MSG_BLOCK, result.MissingParent)
                            }));
                        }
						
                        //TODO: refine possible state (don't reject blocks that were requested by GetTip; reply with DUPLICATE instead of INVALID, ...)
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
					var bk = new GetBlockAction() { BkHash = tip.Key }.Publish().Result;

					if (bk != null)
					{
                        NodeServerTrace.Information("Sending tip: " + tip.Value.header.blockNumber);
						node.SendMessageAsync(bk);
					}
					else
					{
						NodeServerTrace.Information("Missing or invalid tip block requested");
					}
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
					var bk = new GetBlockAction() { BkHash = inventory.Hash }.Publish().Result;

					if (bk != null)
					{
						NodeServerTrace.Information("Sending block: " + bk.header.blockNumber);
						node.SendMessageAsync(bk);
					}
					else
					{
						NodeServerTrace.Information("Missing or invalid block requested");
					}
				}
			});
		}

		#region ICloneable Members

		public override object Clone()
		{
			var behavior = new ChainBehavior(_BlockChain);
			behavior.Nodes = Nodes;
			return behavior;
		}

		#endregion
	}
}