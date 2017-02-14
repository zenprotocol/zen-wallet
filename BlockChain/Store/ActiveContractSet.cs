using System;
using System.Collections.Generic;
using Store;
using System.Linq;

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

		public IEnumerable<byte[]> GetExpiringList(TransactionContext dbTx, uint blockNumber)
		{
#if DEBUG
			All(dbTx).Where(t => t.Value.LastBlock == blockNumber).ToList().ForEach(t => BlockChainTrace.Information($"contract due to expire at {blockNumber}"));
#endif

			return All(dbTx).Where(t => t.Value.LastBlock == blockNumber).Select(t=>t.Key);
		}

		public void DeactivateContracts(TransactionContext dbTx, uint blockNumber, IEnumerable<byte[]> list)
		{
			foreach (byte[] contractHash in list)
			{
				if (IsActive(dbTx, contractHash))
					Remove(dbTx, contractHash);
				else
					throw new Exception();
			}
		}
	}
}