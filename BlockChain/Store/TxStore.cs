using System;
using Consensus;
using System.Linq;
using Store;

namespace BlockChain.Store
{
	public class TxStore : ConsensusTypeStore<Types.Transaction>
	{
		public TxStore() : base("tx")
		{
		}
	}
}