using System;
using Consensus;
using Store;

namespace Wallet.Store
{
	public class KeyStore : EnumerableStore<Key>
	{
		public KeyStore() : base("key")
		{
		}

		protected override Key Unpack(byte[] data, byte[] key)
		{
			return MessagePacker.Instance.Unpack<Key>(data);
		}

		protected override byte[] Pack(Key item)
		{
			return MessagePacker.Instance.Pack<Key>(item);
		}
	}
}
