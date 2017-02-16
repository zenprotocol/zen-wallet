using Consensus;
using System.Linq;
using System;
using System.Collections.Generic;

namespace BlockChain.Data
{
	public class TxPool : TxPoolBase
	{
		public new void Add(byte[] txHash, TransactionValidation.PointedTransaction ptx)
		{
			new NewTxMessage(txHash, ptx, TxStateEnum.Unconfirmed).Publish();
			new HandleOrphansOfTxAction(txHash).Publish();
			base.Add(txHash, ptx);
		}

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

		public void MoveToICTxPool(HashSet activeContracts)
		{
			foreach (var item in this)
			{
				byte[] contractHash;
				if (BlockChain.IsContractGeneratedTx(item.Value, out contractHash) && !activeContracts.Contains(contractHash))
				{
					BlockChainTrace.Information("inactive contract-generated tx moved to ICTxPool");
					Remove(item.Key);
					ICTxPool.Add(item.Key, item.Value);
				}
			}
		}
	}
}