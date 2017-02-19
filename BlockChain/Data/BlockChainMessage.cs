using System.Collections.Generic;
using Consensus;
using Infrastructure;

namespace BlockChain.Data
{
	public abstract class BlockChainMessage {
		public void Publish()
		{
			MessageProducer<BlockChainMessage>.Instance.PushMessage(this);
		}
	}

	public class BlockMessage : BlockChainMessage
	{
		public bool IsTip { get; set; }
		public HashDictionary<TransactionValidation.PointedTransaction> PointedTransactions { get; set; }

		public BlockMessage(HashDictionary<TransactionValidation.PointedTransaction> pointedTransactions, bool isTip = false)
		{
			PointedTransactions = pointedTransactions;
			IsTip = isTip;
		}
	}

	public class TxMessage : BlockChainMessage
	{
		public byte[] TxHash { get; set; }
		public TransactionValidation.PointedTransaction Ptx { get; set; }
		public TxStateEnum State { get; set; }

		public TxMessage(byte[] txHash, TransactionValidation.PointedTransaction ptx, TxStateEnum state)
		{
			BlockChainTrace.Information("To wallet: " + state, txHash);
			TxHash = txHash;
			Ptx = ptx;
			State = state;
		}
	}
}