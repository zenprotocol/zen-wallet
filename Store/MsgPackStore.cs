using System;
using MsgPack.Serialization;
using Store;

namespace Store
{
	public class MsgPackStore<T> : Store<T> where T : class
	{
		SerializationContext _Context;

		public MsgPackStore(String tableName) : base(tableName)
		{
			_Context = SerializationContext.Default;
		}

		public MsgPackStore(SerializationContext context, String tableName) : base(tableName)
		{
			_Context = context;
		}

		protected override T Unpack(byte[] data, byte[] key)
		{
			return _Context.GetSerializer<T>().UnpackSingleObject(data);
		}

		protected override byte[] Pack(T item)
		{
			return _Context.GetSerializer<T>().PackSingleObject(item);
		}
	}
}
