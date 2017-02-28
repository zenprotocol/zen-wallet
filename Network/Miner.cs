using System;
using System.Threading;
using NBitcoin.Protocol.Behaviors;
using Infrastructure;
using Consensus;
using NBitcoin.Protocol;

namespace Network
{
	public class Miner : ResourceOwner
	{
		private BlockChain.BlockChain _BlockChain;

//		public class NewMinedBlockMessage { public Types.Block Block { get; set; }}
		public BlockBroadcastHub BlockBroadcastHub { get; set; }

		public Miner(BlockChain.BlockChain blockChain) { 
			_BlockChain = blockChain;

			OwnResource (new Timer (o => {
				CreateBlock ();
			}, null, 0, (int)TimeSpan.FromSeconds (10).TotalMilliseconds));
		}
			
		private void CreateBlock()
		{
			if (_BlockChain == null) {
				return;
			}
			try
			{
				var newBlock = _BlockChain.MineAllInMempool();

				if (newBlock != null)
				{
					NodeServerTrace.Information(" ****** new block created ******* threadid=" + GetHashCode());
					BlockBroadcastHub.BroadcastBlockAsync(newBlock);
				}
			}
			catch (Exception e)
			{
				NodeServerTrace.Information("error trying to create block: " + e);
			}
		}
	}
}

