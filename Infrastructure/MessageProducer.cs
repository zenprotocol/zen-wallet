using System;
using System.Collections.Generic;

namespace Infrastructure
{
	public class MessageProducer<T> : Singleton<MessageProducer<T>>
	{
		List<IMessageListener<T>> _Listeners = new List<IMessageListener<T>>();
		public IDisposable AddMessageListener(IMessageListener<T> listener)
		{
			if(listener == null)
				throw new ArgumentNullException("listener");
			lock(_Listeners)
			{
				return new Scope(() =>
					{
						_Listeners.Add(listener);
					}, () =>
					{
						lock(_Listeners)
						{
							_Listeners.Remove(listener);

							if (listener is IDisposable) {
								((IDisposable)listener).Dispose();
							}
						}
					});
			}
		}

		public void PushMessage(T message)
		{
			if(message == null)
				throw new ArgumentNullException("message");
			lock(_Listeners)
			{
				foreach(var listener in _Listeners)
				{
					listener.PushMessage(message);
				}
			}
		}
			
		public void PushMessages(IEnumerable<T> messages)
		{
			if(messages == null)
				throw new ArgumentNullException("messages");
			lock(_Listeners)
			{
				foreach(var message in messages)
				{
					if(message == null)
						throw new ArgumentNullException("message");
					foreach(var listener in _Listeners)
					{
						listener.PushMessage(message);
					}
				}
			}
		}
	}
}

