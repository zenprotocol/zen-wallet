using System;
using Consensus;
using Store;
using Wallet.core.Data;

namespace Wallet.core.Store
{
	public class KeyStore : MsgPackStore<Key>
	{
		public KeyStore() : base("key")
		{
		}
	}
}
