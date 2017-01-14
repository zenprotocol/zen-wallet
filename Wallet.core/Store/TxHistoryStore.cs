using System;
using Store;
using Consensus;
using BlockChain.Store;

namespace Wallet.core.Store
{
	public class TxHistoryStore : ConsensusTypeStore<Types.Transaction>
	{
		public TxHistoryStore() : base("tx") { }
	}
}
