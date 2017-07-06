using System;
using Consensus;
using System.Linq;

namespace BlockChain.Data
{
    public abstract class KnownTxPool : TxPoolBase<TransactionValidation.PointedTransaction>
    {
		public OrphanTxPool OrphanTxPool { get; set; }

		public void MoveToOrphans(byte[] txHash)
        {
            if (Contains(txHash))
            {
				var ptx = this[txHash];

                Remove(txHash);

                var tx = TransactionValidation.unpoint(ptx);

                OrphanTxPool.Add(txHash, tx);
			}
        }

		public void MoveToOrphansWithDependencies(byte[] txHash)
		{
			if (Contains(txHash))
			{
                var deps = GetDependencies(txHash).Where(t=>t.Item1 is KnownTxPool);

                MoveToOrphans(txHash);

                foreach (var t in deps)
                {
                    ((KnownTxPool)t.Item1).MoveToOrphansWithDependencies(t.Item2);
                }
			}
		}
    }
}