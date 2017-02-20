using System;
using System.Collections.Generic;
using Store;
using System.Linq;
using BlockChain.Data;

namespace BlockChain
{
	public class ACSItem
	{
		public byte[] Hash { get; set; }
		public string AssemblyFile { get; set; }
		public byte[] CostFn { get; set; }
		public ulong KalapasPerBlock { get; set; }
		public UInt32 LastBlock { get; set; }
	}

	public class ActiveContractSet : MsgPackStore<ACSItem>
	{
		public ActiveContractSet() : base("contract-set") { }

		public void Add(TransactionContext dbTx, ACSItem item)
		{
			Put(dbTx, new Keyed<ACSItem>(item.Hash, item));
		}

		public HashSet Keys(TransactionContext dbTx)
		{
			return new HashSet(All(dbTx).Select(t => t.Value.Hash));
		}

		public bool IsActive(TransactionContext dbTx, byte[] contractHash)
		{
			return ContainsKey(dbTx, contractHash);
		}

		public void Extend(TransactionContext dbTx, byte[] contractHash, ulong kalapas)
		{
			var acsItem = Get(dbTx, contractHash);
			acsItem.Value.LastBlock += (uint)(kalapas / acsItem.Value.KalapasPerBlock);

			Put(dbTx, acsItem);
		}

		public HashDictionary<ACSItem> GetExpiringList(TransactionContext dbTx, uint blockNumber)
		{
#if TRACE
			All(dbTx).Where(t => t.Value.LastBlock == blockNumber).ToList().ForEach(t => BlockChainTrace.Information($"contract due to expire at {blockNumber}", t.Key));
#endif

			var values = new HashDictionary<ACSItem>();

			foreach (var contract in All(dbTx).Where(t => t.Value.LastBlock == blockNumber))
			{
				values[contract.Key] = contract.Value;
			}

			return values;
		}

		public void DeactivateContracts(TransactionContext dbTx, IEnumerable<byte[]> list)
		{
			foreach (var item in list)
			{
				if (IsActive(dbTx, item))
					Remove(dbTx, item);
				else
					throw new Exception();
			}
		}
	}
}