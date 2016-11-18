//using System;
//using BlockChain.Data;
//using Consensus;

//namespace BlockChain
//{
//	public class KeyedTransaction : Keyed<Types.Transaction>
//	{
//		public KeyedTransaction(Types.Transaction transaction) : base(Merkle.transactionHasher.Invoke(transaction), transaction)
//		{
//		}

//		public KeyedTransaction(Types.Transaction transaction, byte[] key) : base(Merkle.transactionHasher.Invoke(transaction), transaction)
//		{
//		}
//	}
//}
