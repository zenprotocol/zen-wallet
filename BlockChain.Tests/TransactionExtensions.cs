using Consensus;
using Microsoft.FSharp.Collections;

namespace BlockChain
{
	public static class TransactionExtensions
	{
		public static Types.Transaction Tag(this Types.Transaction tx, string value)
		{
			BlockChainTrace.SetTag(tx, value);
			return tx;
		}

		public static Types.Transaction AddOutput(this Types.Transaction tx, byte[] address, byte[] asset, ulong amount)
		{
			return tx.AddOutput(Utils.GetOutput(address, asset, amount));
		}

		public static Types.Transaction AddOutput(this Types.Transaction tx, Types.Output output)
		{
			return new Types.Transaction(tx.version,
										 tx.inputs,
										 tx.witnesses,
			                             FSharpList<Types.Output>.Cons(output, tx.outputs),
										 tx.contract);
		}

		public static Types.Transaction SetContract(this Types.Transaction tx, Types.ExtendedContract extendedContract)
		{
			return new Types.Transaction(tx.version,
										 tx.inputs,
										 tx.witnesses,
										 tx.outputs,
										 new Microsoft.FSharp.Core.FSharpOption<Types.ExtendedContract>(extendedContract));
		}

		public static Types.Transaction SetContract(this Types.Transaction tx, Types.Contract contract)
		{
			return new Types.Transaction(tx.version,
										 tx.inputs,
										 tx.witnesses,
										 tx.outputs,
										 new Microsoft.FSharp.Core.FSharpOption<Types.ExtendedContract>(
				                            Types.ExtendedContract.NewContract(contract)));
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

		public static byte[] Key(this Types.Transaction tx)
		{
			return Merkle.transactionHasher.Invoke(tx);
		}
	}
}
