using System;
using Consensus;
using Store;

namespace Wallet.Store
{
	public class KeyStore : Store<Key>
	{
		public KeyStore() : base("key")
		{
		}

		protected override StoredItem<Key> Wrap(Key item)
		{
			var data = MessagePacker.Instance.Pack<Key>(item);
			var key = Merkle.innerHash(data);

			return new StoredItem<Key>(key, item, data);
		}

		protected override Key FromBytes(byte[] data, byte[] key)
		{
			return MessagePacker.Instance.Unpack<Key>(data);
		}
	}
}
