using System;
using Store;
using Consensus;
using MsgPack;
using MsgPack.Serialization;

namespace BlockChain.Store
{
	public class ContractStore : ConsensusTypeStore<Types.Contract>
	{
		public ContractStore() : base("contract")
		{
		}
	}
}