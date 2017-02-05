using System;
using Consensus;
using Store;

namespace BlockChain.Store
{
	public class ChainTip : KeylessValueStore<byte[]>
	{
		public ChainTip() : base("chainTip")
		{
		}
	}
}
