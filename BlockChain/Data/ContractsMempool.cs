using System;

namespace BlockChain.Data
{
	public class ContractsMempoolItem
	{
		byte[] Hash { get; set; }
		string AssemblyFile { get; set; }
		byte[] CostFn { get; set; }
		ulong Cost { get; set; }
		HashSet LastBlock { get; set; }
	}

	public class ContractsMempool
	{
		private readonly HashDictionary<ContractsMempoolItem> _Contracts;

		public ContractsMempool()
		{
		}
	}
}
