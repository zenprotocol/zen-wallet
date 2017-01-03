using System;
using Store;
using Consensus;
using BlockChain.Store;

namespace Wallet.core.Store
{
	public class TxHistoryStore : ConsensusTypeStore<Types.Transaction>
	{
		DBContext _DBContext;

		public TxHistoryStore(DBContext dbContext) : base("tx")
		{
			_DBContext = dbContext;
		}

		public void Put(Types.Transaction t) {
			var key = Merkle.transactionHasher.Invoke (t);
			var keyed = new Keyed<Types.Transaction> (key, t);

			base.Put (_DBContext.GetTransactionContext(), keyed);
		}
	}
}
