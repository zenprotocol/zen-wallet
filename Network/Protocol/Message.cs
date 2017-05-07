using System;
using MsgPack.Serialization;

namespace NBitcoin.Protocol
{
	public class Message
	{
		private Object _Payload { get; set; }

		public Message(Object Payload)
		{
			_Payload = Payload;
		}

		public override string ToString()
		{
			return string.Format($"[Message: Payload={_Payload}]");
		}

		public bool IfPayloadIs<T>(Action<T> action = null) where T : class
		{
			var payload = _Payload as T;
			if (payload != null)
				action(payload);
			return payload != null;
		}

		internal T AssertPayload<T>()
		{
			if (_Payload is T)
				return (T)(_Payload);
			else
			{
				var ex = new ProtocolException("Expected message " + typeof(T).Name + " but got " + _Payload.GetType().Name);
				throw ex;
			}
		}

		internal bool IsPayloadTypeOf(params Type[] types)
		{
			foreach (Type type in types)
			{
				if (_Payload.GetType().Equals(type)) {
					return true;
				}
			}
			return false;
		}
	}
}
