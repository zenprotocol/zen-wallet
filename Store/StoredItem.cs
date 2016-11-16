using System;
namespace Store
{
	public class Keyed<T>
	{
		public T Value { get; private set; }
		public byte[] Key { get; private set; }

		public Keyed(T value, byte[] key)
		{
			Value = value;
			Key = key;
		}
	}

	public class StoredItem<T> : Keyed<T>
	{
		public byte[] Data { get; private set; }

		public StoredItem(T value, byte[] key, byte[] data) : base(value, key)
		{
			Data = data;
		}
	}
}
