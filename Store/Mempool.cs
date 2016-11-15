using System;
using Consensus;
using Store;

namespace Blockchain
{
	public class Mempool : CachedStore<TransactionStore, Types.Transaction>
	{
		public Mempool() : base("db", "mempool")
		{
		}
	}
}
