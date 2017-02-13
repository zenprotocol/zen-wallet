using System;

namespace BlockChain.Data
{
	public class ContractsMempoolItem
	{
		public byte[] Hash { get; set; }
		public string AssemblyFile { get; set; }
		public byte[] CostFn { get; set; }
		public ulong Cost { get; set; }
		public HashSet LastBlock { get; set; }
	}

	public class ContractsMempool
	{
		private readonly HashDictionary<ContractsMempoolItem> _Contracts = new HashDictionary<ContractsMempoolItem>();

		public void Add(ContractsMempoolItem item)
		{
			_Contracts[item.Hash] = item;
		}

		public void RemoveTx(byte[] txHash)
		{
			foreach (var item in _Contracts)
			{
				if (item.Value.LastBlock.Contains(txHash))
				{
					item.Value.LastBlock.Remove(txHash);
				}

				if (item.Value.LastBlock.Count == 0)
				{
					_Contracts.Remove(item.Key); //TODO: messing with iterator?
				}
			}
		}
	}
}
