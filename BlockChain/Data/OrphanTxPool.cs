using System.Collections.Generic;
using BlockChain.Data;
using Consensus;
using System.Linq;
using Store;
using Infrastructure;
using System;

namespace BlockChain.Data
{
    public class OrphanTxPool : TxPoolBase<Types.Transaction>
	{
        public override IEnumerable<Tuple<IPool, byte[]>> GetDependencies(byte[] txHash)
		{
			foreach (var item in this)
			{
				if (item.Value.inputs.Any(t => t.txHash.SequenceEqual(txHash)))
				{
					yield return new Tuple<IPool, byte[]>(this, item.Key);
				}
			}
		}

        public override bool IsDoubleSpend(Types.Transaction t, IEnumerable<Types.Outpoint> spentOutputs)
		{
            return t.inputs.Any(_t => spentOutputs.Contains(_t));
		}
    }
}