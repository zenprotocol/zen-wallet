using System;
using System.Linq;
using Consensus;
using Microsoft.FSharp.Collections;
using Store;

namespace Infrastructure.Testing
{
	public static class TransactionExtensions
	{
		public static Types.Transaction AddOutput(this Types.Transaction tx, byte[] address, byte[] asset, ulong amount)
		{
			return tx.AddOutput(new Types.Output(
				Types.OutputLock.NewPKLock(address), 
				new Types.Spend(asset, amount)));
		}

		public static Types.Transaction AddOutput(this Types.Transaction tx, Types.Output output)
		{
			var outputs = tx.outputs.ToList();

			outputs.Add(output);

			return new Types.Transaction(tx.version,
										 tx.inputs,
										 tx.witnesses,
										 ListModule.OfSeq(outputs),
										 tx.contract);
		}

		public static Types.Transaction AddInput(this Types.Transaction tx, Types.Transaction txRef, int index)
		{
			return tx.AddInput(new Types.Outpoint(Merkle.transactionHasher.Invoke(txRef), (uint)index));
		}

		public static Types.Transaction AddInput(this Types.Transaction tx, byte[] txHash, int index)
		{
			return tx.AddInput(new Types.Outpoint(txHash, (uint)index));
		}

		public static Types.Transaction AddInput(this Types.Transaction tx, Types.Outpoint outpoint)
		{
			var inputs = tx.inputs.ToList();

			inputs.Add(outpoint);

			return new Types.Transaction(tx.version,
										 ListModule.OfSeq(inputs),
										 tx.witnesses,
			                             tx.outputs,
										 tx.contract);
		}
	}
}
