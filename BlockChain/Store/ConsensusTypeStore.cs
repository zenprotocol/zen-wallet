using System;
using Consensus;
using Store;

namespace BlockChain.Store
{
	public class ConsensusTypeStore<T> : MsgPackStore<T> where T : class
	{
		public ConsensusTypeStore(String tableName) : base(Serialization.context, tableName)
		{
		}
	}
}
