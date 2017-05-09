using System;
using System.Linq;
using System.Collections.Generic;
using Consensus;
using Store;

namespace BlockChain.Data
{
	public class ICTxPool : TxPoolBase
	{
		//public TxPool TxPool { get; set; }

		//public void Purge(HashSet activeContracts, TransactionContext dbTx, Types.BlockHeader blockHeader)
		//{
		//	foreach (var key in Keys.ToList())
		//	{
		//		var tx = this[key];
		//		var contractHash = ((Types.OutputLock.ContractLock)tx.pInputs.Head.Item2.@lock).contractHash;

		//		if (activeContracts.Contains(contractHash) && ContractHelper.IsTxValid(tx, contractHash, utxos, blockHeader))
		//		{
		//			Remove(key);
		//			TxPool.Add(key, tx);
		//			new TxMessage(key, tx, TxStateEnum.Unconfirmed).Publish();
		//			new HandleOrphansOfTxAction(key).Publish();
		//			// todo check if ptx **activates a contract** and update contractpool if it does
		//		}
		//	}
		//}
	}
}