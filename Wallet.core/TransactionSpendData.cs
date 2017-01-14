using System.Collections.Generic;
using BlockChain.Data;
using Consensus;
using Microsoft.FSharp.Collections;
using Wallet.core.Data;

namespace Wallet.core
{
	public class TransactionSpendData
	{
		public TransactionValidation.PointedTransaction PointedTransaction { get; protected set; }
		public List<int> Outputs { get; private set; }
		public List<int> Inputs { get; private set; }
		public HashDictionary<long> Balances { get; private set; }

		protected TransactionSpendData(TransactionValidation.PointedTransaction pointedTransaction, List<int> outputs, List<int> inputs)
		{
			PointedTransaction = pointedTransaction;
			Balances = new HashDictionary<long>();

			Outputs = outputs;
			Inputs = inputs;

			foreach (var inputIndex in Inputs)
			{
				var input = PointedTransaction.pInputs[inputIndex];

				if (!Balances.ContainsKey(input.Item2.spend.asset))
				{
					Balances[input.Item2.spend.asset] = 0;
				}
				Balances[input.Item2.spend.asset] -= (long)input.Item2.spend.amount;
			}

			foreach (var outputIndex in Outputs)
			{
				var output = PointedTransaction.outputs[outputIndex];

				if (!Balances.ContainsKey(output.spend.asset))
				{
					Balances[output.spend.asset] = 0;
				}
				Balances[output.spend.asset] += (long)output.spend.amount;
			}
		}
	}

	public class TransactionSpendDataEx : TransactionSpendData
	{
		public Types.Transaction Transaction { get; private set; }
		public List<Key> Keys { get; private set; }

		public TransactionSpendDataEx(Types.Transaction transaction, List<Types.Output> outputs_, List<int> outputs, List<int> inputs, List<Key> keys)
			: base(TransactionValidation.toPointedTransaction(transaction, ListModule.OfSeq(outputs_)), outputs, inputs)
		{
			Transaction = transaction;
			Keys = keys;
		}
	}
}
