using System;
using System.Threading;
using NBitcoin.Protocol.Behaviors;
using Infrastructure;
using Consensus;

namespace NBitcoinDerive
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

			var newBlock = _BlockChain.MineAllInMempool();

			if (newBlock != null) {
				Console.WriteLine (" ****** new block created ******* by " + GetHashCode());
	//			MessageProducer.PushMessage(new NewMinedBlockMessage() { Block = newBlock });
				BlockBroadcastHub.BroadcastBlockAsync (newBlock);
		//	} {
		//		Console.WriteLine (" ****** no new block created !!! ******* by " + GetHashCode());
			}
		}
	}
}

