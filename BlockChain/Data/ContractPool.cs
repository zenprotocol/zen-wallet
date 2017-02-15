using Store;
using System.Linq;
using Consensus;
using System.Collections.Generic;
using System;

namespace BlockChain.Data
{
	public class ContractsPoolItem
	{
		public byte[] Hash { get; set; }
		public string AssemblyFile { get; set; }
		public byte[] CostFn { get; set; }
		public ulong Cost { get; set; }
		public int Refs { get; set; }
	}

	public class ContractPool
	{
		readonly HashDictionary<ContractsPoolItem> _Contracts = new HashDictionary<ContractsPoolItem>();
		readonly HashDictionary<byte[]> _Txs = new HashDictionary<byte[]>();

		public void RemoveRef(byte[] txHash, TransactionContext dbTx, TxPool txMempool)
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

					if (new ActiveContractSet().IsActive(dbTx, contractHash))
						return;

					txMempool.InactivateContractGeneratedTxs(dbTx, contractHash);
				}
			}
		}

		internal void LookForActivated(TransactionContext dbTx)
		{
			//foreach (var contract in _Contracts)
			//{
			//	if (new ActiveContractSet().IsActive(dbTx, contract.Key))
			//	{
			//		BlockChain.IsContractGeneratedTransactionValid(dbTx, 
			//	}
			//}
		}
	}
}
