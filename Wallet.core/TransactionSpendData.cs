//using System.Collections.Generic;
//using BlockChain.Data;
//using Consensus;
//using Microsoft.FSharp.Collections;
//using Wallet.core.Data;

//namespace Wallet.core
//{
//	public class TransactionsBalanceData : HashDictionary<TransactionBalanceData>
//	{
//	}

//	public class TransactionBalanceData
//	{
//		public HashDictionary<ulong> Balances { get; set; }
//		public byte[] TransactionHash { get; set; }
//	}
//}
//	//public class BalanceData
//	//{
//	//	public byte[] Asset { get; set; }
//	//	public ulong Amount { get; set; }
//	//}

////	public class TransactionSpendData
////	{
////		public TransactionValidation.PointedTransaction PointedTransaction { get; protected set; }
////		public List<uint> Outputs { get; private set; }
////		public List<uint> Inputs { get; private set; }
////		public HashDictionary<ulong> Balances { get; private set; }

////		protected TransactionSpendData(TransactionValidation.PointedTransaction pointedTransaction, List<uint> outputs, List<uint> inputs)
////		{
////			PointedTransaction = pointedTransaction;
////			Balances = new HashDictionary<ulong>();

////			Outputs = outputs;
////			Inputs = inputs;

////			foreach (var inputIndex in Inputs)
////			{
////				var input = PointedTransaction.pInputs[(int)inputIndex];

////				if (!Balances.ContainsKey(input.Item2.spend.asset))
////				{
////					Balances[input.Item2.spend.asset] = 0;
////				}
////				Balances[input.Item2.spend.asset] -= input.Item2.spend.amount;
////			}

////			foreach (var outputIndex in Outputs)
////			{
////				var output = PointedTransaction.outputs[(int)outputIndex];

////				if (!Balances.ContainsKey(output.spend.asset))
////				{
////					Balances[output.spend.asset] = 0;
////				}
////				Balances[output.spend.asset] += output.spend.amount;
////			}
////		}
////	}

////	public class TransactionSpendDataEx : TransactionSpendData
////	{
////		public Types.Transaction Transaction { get; private set; }
////		public List<Key> Keys { get; private set; }

////		public TransactionSpendDataEx(Types.Transaction transaction, List<Types.Output> outputs_, List<uint> outputs, List<uint> inputs, List<Key> keys)
////			: base(TransactionValidation.toPointedTransaction(transaction, ListModule.OfSeq(outputs_)), outputs, inputs)
////		{
////			Transaction = transaction;
////			Keys = keys;
////		}
////	}
////}
