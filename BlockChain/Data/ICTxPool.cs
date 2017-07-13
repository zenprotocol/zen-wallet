using System;
using System.Linq;
using System.Collections.Generic;
using Consensus;
using Store;

namespace BlockChain.Data
{
    public class ICTxPool : KnownTxPool
    {
		public override IEnumerable<Tuple<IPool, byte[]>> GetDependencies(byte[] txHash)
		{
			foreach (var item in this)
			{
				if (item.Value.pInputs.Select(t => t.Item1).Any(t => t.txHash.SequenceEqual(txHash)))
				{
					yield return new Tuple<IPool, byte[]>(this, item.Key);
				}
			}

			foreach (var item in OrphanTxPool.GetDependencies(txHash))
			{
				yield return item;
			}
		}

		public override bool IsDoubleSpend(TransactionValidation.PointedTransaction t, IEnumerable<Types.Outpoint> spentOutputs)
		{
			return t.pInputs.Select(_t => _t.Item1).Any(_t => spentOutputs.Contains(_t));
		}
    }
}