using Store;
using System.Linq;
using Consensus;
using System.Collections.Generic;

namespace BlockChain.Data
{
	public class ContractsMempoolItem
	{
		public byte[] Hash { get; set; }
		public string AssemblyFile { get; set; }
		public byte[] CostFn { get; set; }
		public ulong Cost { get; set; }
		public int Refs { get; set; }
	}

	public class ContractsMempool
	{
		readonly HashDictionary<ContractsMempoolItem> _Contracts = new HashDictionary<ContractsMempoolItem>();
		readonly HashDictionary<byte[]> _Txs = new HashDictionary<byte[]>();

		public void RemoveRef(byte[] txHash, TransactionContext dbTx, ActiveContractSet acs, ulong height, TxMempool txMempool)
		{
			lock (_Txs)
			{
				if (!_Txs.ContainsKey(txHash))
					return;

				var contractHash = _Txs[txHash];
				var contract = _Contracts[contractHash];

				_Txs.Remove(txHash);
				contract.Refs--;

				if (contract.Refs == 0)
				{
					_Contracts.Remove(contractHash);

					if (acs.IsActive(dbTx, contractHash, height))
						return;

					var toInactivate = new List<byte[]>();
					foreach (var tx in txMempool.Transactions)
					{
						byte[] txContractHash = null;
						if (BlockChain.IsContractGeneratedTx(tx.Value, out txContractHash) && contractHash.SequenceEqual(txContractHash))
							toInactivate.Add(tx.Key);
					}

					toInactivate.ForEach(txMempool.MoveToInactiveContractGeneratedTxs);
				}
			}
		}
	}
}
