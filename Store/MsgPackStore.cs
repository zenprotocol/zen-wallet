using System;
using MsgPack.Serialization;
using Store;

namespace Store
{
	public class MsgPackStore<TKey, TValue> : Store<TKey, TValue> //where T : class
	{
		protected SerializationContext _Context;

		public MsgPackStore(String tableName) : base(tableName)
		{
			_Context = SerializationContext.Default;
		}

		public MsgPackStore(SerializationContext context, String tableName) : base(tableName)
		{
			_Context = context;
		}

		protected override byte[] Pack<T>(T value)
		{
			return _Context.GetSerializer<T>().PackSingleObject(value);
		}

		protected override T Unpack<T>(byte[] data)
		{
			return _Context.GetSerializer<T>().UnpackSingleObject(data);
		}
	}

	public class MsgPackStore<TValue> : MsgPackStore<byte[], TValue>
	{
		public MsgPackStore(String tableName) : base(tableName)
		{
		}
	}
}
