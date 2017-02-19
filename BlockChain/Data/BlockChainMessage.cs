using System.Collections.Generic;
using Consensus;
using Infrastructure;

namespace BlockChain.Data
{
	public class BlockChainMessage {
		public void Publish()
		{
			MessageProducer<BlockChainMessage>.Instance.PushMessage(this);
		}
	}

	public class NewBlockMessage : BlockChainMessage
	{
		public bool IsTip { get; set; }
		//public Types.Block Bk { get; set; }
		public List<TransactionValidation.PointedTransaction> PointedTransactions { get; set; }

		public NewBlockMessage(/*Types.Block bk,*/ List<TransactionValidation.PointedTransaction> pointedTransactions, bool isTip = false)
		{
		//	Bk = bk;
			PointedTransactions = pointedTransactions;
			IsTip = IsTip;
		}
	}

	public class NewTxMessage : BlockChainMessage
	{
		public byte[] TxHash { get; set; }
		public TransactionValidation.PointedTransaction Tx { get; set; }
		public TxStateEnum State { get; set; }

		public NewTxMessage(byte[] txHash, TxStateEnum state)
		{
			BlockChainTrace.Information("To wallet: " + state, txHash);
			TxHash = txHash;
			State = state;
		}

		public NewTxMessage(byte[] txHash, TransactionValidation.PointedTransaction tx, TxStateEnum state)	
		{
			BlockChainTrace.Information("To wallet: " + state, tx);
			TxHash = txHash;
			Tx = tx;
			State = state;
		}
	}
}