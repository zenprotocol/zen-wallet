using System;
using System.Threading;
using NBitcoin.Protocol.Behaviors;
using Consensus;
using Microsoft.FSharp.Collections;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace Network
{
	public class MinerLogData
	{
		public uint BlockNumber { get; set; }
		public int Transactions { get; set; }
		public double TimeToMine { get; set; }
		public BlockChain.BlockVerificationHelper.BkResultEnum Status { get; set; }
	}

	public class Miner
	{
		Thread _Thread;
		bool _Enabled;
		bool _Stopping;

		public event Action<MinerLogData> OnMinedBlock;

		public int Difficulty { get; set; }
		public bool Enabled { 
			get
			{
				return _Enabled;
			}
			set {
				_Enabled = value;

				if (value)
				{
					if (!_Thread.IsAlive)
					{
						_Stopping = false;
						_Thread.Start();
					}
				}
				else
				{
					if (_Thread.IsAlive)
					{
						_Stopping = true;
						_Thread.Join();
					}
				}
			} 
		}
		public BlockChain.BlockChain BlockChain_ { get; set; }
		public BlockBroadcastHub BlockBroadcastHub { get; set; }

		public Miner()
		{
			Difficulty = (int) (8 * 3.5);

			_Thread = new Thread(() =>
			{
				try
				{
					while (!_Stopping)
					{
						Mine(Difficulty);
					}
				}
				catch (ThreadInterruptedException tie)
				{
				}
			});
		}

#if DEBUG
		public
#endif
		void Mine(int difficulty = 0)
		{
			uint version = 1;
			var nonce = new byte[10];
			var random = new Random();
			var time = DateTime.Now.ToUniversalTime();

			while (!_Stopping)
			{
				var tip = BlockChain_.Tip;

				if (tip == null || BlockChain_.memPool.TxPool.Count == 0)
				{
					Thread.Sleep(10);
					continue;
				}

				var txs = BlockChain_.memPool.TxPool.Select(t => TransactionValidation.unpoint(t.Value));
				var txsList = ListModule.OfSeq(txs);

				var txMerkleRoot = Merkle.merkleRoot(
					new byte[] { },
					Merkle.transactionHasher,
					txsList
				);

				random.NextBytes(nonce);

				var blockHeader = new Types.BlockHeader(
					version,
					tip.Key,
					tip.Value.header.blockNumber + 1,
					txMerkleRoot,
					new byte[] { },
					new byte[] { },
					ListModule.OfSeq<byte[]>(new List<byte[]>()),
					DateTime.Now.ToUniversalTime().Ticks,
					0,
					nonce
				);

				var bkHash = Merkle.blockHeaderHasher.Invoke(blockHeader);

				var c = 0;

				if (difficulty != 0)
				{
					var bits = new BitArray(bkHash);
					var len = bits.Length - 1;
					for (var i = 0; i < len; i++)
						if (bits[len - i])
							c++;
						else
							break;
				}

				if (c >= difficulty)
				{
					var log = new MinerLogData();
					var block = new Types.Block(blockHeader, txsList);

					log.TimeToMine = (DateTime.Now.ToUniversalTime() - time).TotalSeconds;
					log.BlockNumber = block.header.blockNumber;
					log.Transactions = block.transactions.Count();

					var accpeted = BlockChain_.HandleBlock(block);
					log.Status = accpeted;

					if (accpeted == BlockChain.BlockVerificationHelper.BkResultEnum.Accepted && BlockBroadcastHub != null)
						BlockBroadcastHub.BroadcastBlockAsync(block);

					if (OnMinedBlock != null)
						OnMinedBlock(log);

					return;
				}
			}
		}
	}
}

