using Consensus;
using Infrastructure;

namespace BlockChain.Data
{
	public class Message {
		public void Publish()
		{
			MessageProducer<Message>.Instance.PushMessage(this);
		}
	}

	public class NewBlockMessage : Message
	{
		public Types.Block Bk { get; set; }

		public NewBlockMessage(Types.Block bk)
		{
			Bk = bk;
		}
	}

	public class NewTipMessage : NewBlockMessage
	{
		public NewTipMessage(Types.Block bk) : base(bk)
		{
		}
	}

	public class NewTxMessage : Message
	{
		public TransactionValidation.PointedTransaction Tx { get; set; }

		public NewTxMessage(TransactionValidation.PointedTransaction tx)	
		{
			Tx = tx;
		}
	}

	public class TxInvalidatedMessage : NewTxMessage
	{
		public TxInvalidatedMessage(TransactionValidation.PointedTransaction tx) : base(tx)
		{
		}
	}
}