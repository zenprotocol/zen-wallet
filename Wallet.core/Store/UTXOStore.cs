using Consensus;
using BlockChain.Store;

namespace Wallet.core.Store
{
	public class UTXOStore : ConsensusTypeStore<Types.Output>
	{
		public UTXOStore() : base("utxo") { }
	}
}
