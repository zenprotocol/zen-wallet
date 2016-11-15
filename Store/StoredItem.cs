using System;
namespace Store
{
	public class StoredItem<T>
	{
		public T Value { get; private set; }
		public byte[] Key { get; private set; }
		public byte[] Data { get; private set; }

		public StoredItem(T value, byte[] key, byte[] data)
		{
			Value = value;
			Key = key;
			Data = data;
		}
	}
}
