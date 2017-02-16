using System.Collections.Generic;
using BlockChain.Data;
using Consensus;
using System.Linq;
using Store;
using Infrastructure;
using System;

namespace BlockChain.Data
{
	public class OrphanTxPool : HashDictionary<Types.Transaction>
	{
		public void RemoveDependencies(byte[] txHash)
		{
			if (ContainsKey(txHash))
			{
				BlockChainTrace.Information("tx removed from orphan pool");
				Remove(txHash);
				foreach (var dep in GetOrphansOf(txHash))
				{
					RemoveDependencies(dep.Item1);
				}
			}
		}

		public IEnumerable<Tuple<byte[], Types.Transaction>> GetOrphansOf(byte[] txHash)
		{
			foreach (var item in this)
			{
				if (item.Value.inputs.Count(t => t.txHash.SequenceEqual(txHash)) > 0)
				{
					yield return new Tuple<byte[], Types.Transaction>(item.Key, item.Value);
				}
			}
		}
	}
}