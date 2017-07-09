using System.Collections.Generic;
using Consensus;
using System.Linq;

namespace BlockChain.Data
{
    public class MemPool
    {
        public readonly ICTxPool ICTxPool = new ICTxPool();
        public readonly TxPool TxPool = new TxPool();
        public readonly OrphanTxPool OrphanTxPool = new OrphanTxPool();
        public readonly ContractPool ContractPool = new ContractPool();

        public MemPool()
        {
            //TODO: remove. keep a single point of truth (only have knowledge of depedencies in "GetDependencies")

			//ICTxPool.TxPool = TxPool;
			ICTxPool.ContractPool = ContractPool;
            ICTxPool.OrphanTxPool = OrphanTxPool;

            TxPool.ICTxPool = ICTxPool;
            TxPool.ContractPool = ContractPool;
            TxPool.OrphanTxPool = OrphanTxPool;
        }

        public void RemoveDoubleSpends(IEnumerable<Types.Outpoint> spentOutputs)
        {
            RemoveDoubleSpends(TxPool, spentOutputs);
            RemoveDoubleSpends(ICTxPool, spentOutputs);
            RemoveDoubleSpends(OrphanTxPool, spentOutputs);
        }

        void RemoveDoubleSpends<T>(TxPoolBase<T> pool, IEnumerable<Types.Outpoint> spentOutputs)
        {
            //TODO: simplify

            pool.Keys.ToList().ForEach(t =>
            {
                if (pool.Contains(t))
                {
                    if (pool.IsDoubleSpend(pool[t], spentOutputs))
                    {
                        //BlockChainTrace.Information("double-spending tx removed from txpool", t);

                        //new TxMessage(t, pool[t], TxStateEnum.Invalid).Publish();
                        pool.RemoveWithDependencies(t);

                      //  pool.GetDependencies(t).ToList().ForEach(RemoveDependencies);
                    }
                }
            });
        }
	}
}