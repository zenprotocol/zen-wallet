using System.Collections.Generic;
using Consensus;
using System;
using System.Linq;

namespace BlockChain.Data
{
    public interface IPool 
    {
        bool RemoveWithDependencies(byte[] txHash);   
    }

	public abstract class TxPoolBase<T> : HashDictionary<T>, IPool
	{
		public ContractPool ContractPool { get; set; }

		public bool Contains(byte[] txHash)
		{
			return ContainsKey(txHash);
		}

		public abstract bool IsDoubleSpend(T t, IEnumerable<Types.Outpoint> spentOutputs);
        public abstract IEnumerable<Tuple<IPool, byte[]>> GetDependencies(byte[] txHash);

		public virtual bool RemoveWithDependencies(byte[] txHash)
		{
            if (Contains(txHash))
            {
                var deps = GetDependencies(txHash).ToList();
            
                Remove(txHash);

                deps.ForEach(t => t.Item1.RemoveWithDependencies(t.Item2));

                return true;
            }

            return false;
		}
    }
}