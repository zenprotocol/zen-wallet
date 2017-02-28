using System;
using Consensus;
using Store;

namespace BlockChain.Store
{
	public class ConsensusTypeStore<TKey, TValue> : MsgPackStore<TKey, TValue>
	{
		public ConsensusTypeStore(String tableName) : base(Serialization.context, tableName)
		{
		}
	}
	
	public class ConsensusTypeStore<TValue> : ConsensusTypeStore<byte[], TValue>
	{
		public ConsensusTypeStore(String tableName) : base(tableName)
		{
		}
	}
}
