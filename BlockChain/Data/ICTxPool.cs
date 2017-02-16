using System;
using System.Collections.Generic;
using Consensus;

namespace BlockChain.Data
{
	public class ICTxPool : TxPoolBase
	{
		public TxPool TxPool { get; set; }

		public void Purge(HashSet activeContracts, List<Tuple<Types.Outpoint, Types.Output>> utxos)
		{
			foreach (var item in this)
			{
				var contractHash = ((Types.OutputLock.ContractLock)item.Value.pInputs.Head.Item2.@lock).contractHash;

				if (activeContracts.Contains(contractHash) && ContractHelper.IsTxValid(item.Value, contractHash, utxos))
				{
					Remove(item.Key);
					TxPool.Add(item.Key, item.Value);
					new MessageAction(new NewTxMessage(item.Key, TxStateEnum.Unconfirmed)).Publish();
					new HandleOrphansOfTxAction(item.Key).Publish();
					// todo check if ptx **activates a contract** and update contractpool if it does
				}
			}
		}
	}
}