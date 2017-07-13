using Consensus;
using System.Linq;
using System;
using System.Collections.Generic;

namespace BlockChain.Data
{
    public class TxPool : KnownTxPool
	{
		public ICTxPool ICTxPool { get; set; }

		public bool ContainsInputs(Types.Transaction tx)
		{
			foreach (var outpoint in tx.inputs)
			{
				if (ContainsOutpoint(outpoint))
				{
					return true;
				}
			}

			return false;
		}

		public bool ContainsOutpoint(Types.Outpoint outpoint)
		{
			foreach (var item in this)
			{
				if (item.Value.pInputs.Select(t => t.Item1).Contains(outpoint))
				{
					return true;
				}
			}

			return false;
		}

        public void MoveToICTxPool(HashDictionary<byte[]> activeContracts)
		{
			foreach (var item in this.ToList())
			{
				byte[] contractHash;

				if (BlockChain.IsContractGeneratedTx(item.Value, out contractHash) == BlockChain.IsContractGeneratedTxResult.ContractGenerated && 
                    !activeContracts.ContainsKey(contractHash))
				{
					BlockChainTrace.Information("inactive contract-generated tx moved to ICTxPool", contractHash);
					Remove(item.Key);
					ICTxPool.Add(item.Key, item.Value);

                    GetDependencies(item.Key)
                        .Where(t => t.Item1 is KnownTxPool)
                        .ToList().ForEach(t => ((KnownTxPool)t.Item1).MoveToOrphansWithDependencies(t.Item2));
				}
			}
		}

        public override IEnumerable<Tuple<IPool, byte[]>> GetDependencies(byte[] txHash)
		{
			foreach (var item in this)
			{
				if (item.Value.pInputs.Select(t => t.Item1).Any(t => t.txHash.SequenceEqual(txHash)))
				{
                    yield return new Tuple<IPool, byte[]>(this, item.Key);
				}
			}

            foreach (var item in ICTxPool.GetDependencies(txHash))
            {
                yield return item;
            }
		}

        public override bool IsDoubleSpend(TransactionValidation.PointedTransaction t, IEnumerable<Types.Outpoint> spentOutputs)
        {
            return t.pInputs.Select(_t => _t.Item1).Any(_t => spentOutputs.Contains(_t));
        }

        public override bool RemoveWithDependencies(byte[] txHash)
        {
            if (base.RemoveWithDependencies(txHash))
            {
				new TxMessage(txHash, null, TxStateEnum.Invalid).Publish();
                return true;
			}

            return false;
        }
    }
}