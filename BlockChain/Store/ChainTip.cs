using System;
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
