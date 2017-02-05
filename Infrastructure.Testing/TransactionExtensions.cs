using System;
using System.Collections.Generic;
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
			return new Types.Transaction(tx.version,
										 tx.inputs,
										 tx.witnesses,
			                             FSharpList<Types.Output>.Cons(output, tx.outputs),
										 tx.contract);
		}

		public static Types.Transaction AddInput(this Types.Transaction tx, Types.Transaction txRef, int index, byte[] key)
		{
			return tx.AddInput(new Types.Outpoint(Merkle.transactionHasher.Invoke(txRef), (uint)index), key);
		}

		public static Types.Transaction AddInput(this Types.Transaction tx, byte[] txHash, int index, byte[] key)
		{
			return tx.AddInput(new Types.Outpoint(txHash, (uint)index), key);
		}

		public static Types.Transaction AddInput(this Types.Transaction tx, Types.Outpoint outpoint, byte[] key)
		{
			return new Types.Transaction(tx.version,
			                             FSharpList<Types.Outpoint>.Cons(outpoint, tx.inputs),
										 tx.witnesses,
			                             tx.outputs,
										 tx.contract);
		}

		public static Types.Transaction Sign(this Types.Transaction tx, params byte[][] keys)
		{
			return TransactionValidation.signTx(tx, ListModule.OfSeq(keys));
		}
	}
}
