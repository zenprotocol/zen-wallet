using System;

namespace BlockChain.Data
{
	public class Keyed<T>
	{
		public T Value { get; private set; }
		public byte[] Key { get; private set; }

		public Keyed(byte[] key, T value)
		{
			Key = key;
			Value = value;
		}
	}

	public class StoredItem<T> : Keyed<T>
	{
		public byte[] Data { get; private set; }

		public StoredItem(byte[] key, T value, byte[] data) : base(key, value)
		{
			Data = data;
		}
	}
}
