using System;
using System.Threading;
using NBitcoin.Protocol.Behaviors;
using Consensus;
using Microsoft.FSharp.Collections;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using NBitcoin.Protocol;
using System.Threading.Tasks;

namespace Network
{
	public class MinerLogData
	{
		public uint BlockNumber { get; set; }
		public int Transactions { get; set; }
		public double TimeToMine { get; set; }
		public BlockChain.BlockVerificationHelper.BkResult Status { get; set; }
	}

    public class Miner : IDisposable
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
			Difficulty = 1; // (int) (8 * 3.5);

			_Thread = new Thread(() =>
			{
				try
				{
					while (!_Stopping)
					{
						Mine(Difficulty);
						Thread.Sleep(100);
					}
				}
				catch (ThreadInterruptedException tie)
				{
					Console.WriteLine(tie.Message);
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
				}
			});

			_Thread.Name = "Miner";
		}

#if DEBUG
        public bool SkipTxs { get; set; }
		public void MineTestBlock()
		{
			Mine(0);
		}
#endif

		bool Mine(int difficulty)
		{
            byte[] txMerkleRoot;
            FSharpList<Types.Transaction> txsList;

#if DEBUG
			if (SkipTxs)
            {
                txMerkleRoot = new byte[] { };
                txsList = FSharpList<Types.Transaction>.Empty;
            }
            else
#endif
			if (BlockChain_.memPool.TxPool.Count == 0)
			{
				return false;
			}
            else
			{
				var txs = BlockChain_.memPool.TxPool.Select(t => TransactionValidation.unpoint(t.Value));
				txsList = ListModule.OfSeq(txs);

				txMerkleRoot = Merkle.merkleRoot(
					new byte[] { },
					Merkle.transactionHasher,
					txsList
				);
			}

			var tip = BlockChain_.Tip;

			if (tip == null)
			{
				NodeServerTrace.Information("Miner: no tip");
				return false;
			}

			uint version = 1;
			var nonce = new byte[10];
			var random = new Random();
			var time = DateTime.Now.ToUniversalTime();

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
				NodeServerTrace.Information("Block puzzle solved!");

				var log = new MinerLogData();
				var block = new Types.Block(blockHeader, txsList);

				log.TimeToMine = (DateTime.Now.ToUniversalTime() - time).TotalSeconds;
				log.BlockNumber = block.header.blockNumber;
				log.Transactions = block.transactions.Count();

				var result = BlockChain_.HandleBlock(block).Result;

                var accepted = result.BkResultEnum == BlockChain.BlockVerificationHelper.BkResultEnum.Accepted;
				log.Status = result;

				if (accepted && BlockBroadcastHub != null)
				{
					BlockBroadcastHub.BroadcastBlockAsync(block);
				}

				if (OnMinedBlock != null)
					OnMinedBlock(log);

				return accepted;
			}
			else
			{
				return false;
			}
		}

        public void Dispose()
        {
            _Stopping = true;
        }
    }
}

