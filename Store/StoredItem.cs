using System;

namespace Store
{
	public class Keyed<TKey, TValue>
	{
		public TValue Value { get; protected set; }
		public TKey Key { get; protected set; }

		public Keyed()
		{ 
		}

		public Keyed(TKey key, TValue value)
		{
			Key = key;
			Value = value;
		}
	}

	public class Keyed<TValue> : Keyed<byte[], TValue>
	{
		public Keyed()
		{
		}

		public Keyed(byte[] key, TValue value)
		{
			Key = key;
			Value = value;
		}
	}
}
