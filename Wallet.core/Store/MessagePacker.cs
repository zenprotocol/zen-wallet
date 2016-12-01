//using System;
//using System.IO;
//using Infrastructure;
//using MsgPack.Serialization;

//namespace Wallet.core.Store
//{
//	public class MessagePacker : Singleton<MessagePacker>
//	{
//		private readonly SerializationContext _Context;

//		public MessagePacker()
//		{
//			_Context = new SerializationContext { SerializationMethod = SerializationMethod.Map }; //TODO: Map?
//		}

//		public T Unpack<T>(byte[] data)
//		{
//			return _Context.GetSerializer<T>().UnpackSingleObject(data);
//		}

//		public byte[] Pack<T>(T obj)
//		{
//			return _Context.GetSerializer<T>().PackSingleObject(obj);
//		}
//	}
//}
