using System;
using System.Threading;
using System.Collections.Concurrent;

namespace Infrastructure
{
	public interface IMessageListener<in T>
	{
		void PushMessage(T message);
	}

	public class MessageListener<T> : IMessageListener<T>
	{
		private Action<T> processMessage;

		public MessageListener(Action<T> processMessage) {
			this.processMessage = processMessage;
		}

		#region MessageListener Members

		public virtual void PushMessage(T message)
		{
			processMessage (message);
		}

		#endregion
	}

	public class EventLoopMessageListener<T> : IMessageListener<T>, IDisposable
	{
		public EventLoopMessageListener(Action<T> processMessage)
		{
			new Thread(new ThreadStart(() =>
				{
					try
					{
						while(!cancellationSource.IsCancellationRequested)
						{
							var message = _MessageQueue.Take(cancellationSource.Token);
							if(message != null)
							{
								try
								{
									processMessage(message);
								}
								catch(Exception ex)
								{
									Console.WriteLine("Unexpected expected during message loop", ex);
									//NodeServerTrace.Error("Unexpected expected during message loop", ex);
								}
							}
						}
					}
					catch(OperationCanceledException)
					{
					}
				})).Start();
		}
		BlockingCollection<T> _MessageQueue = new BlockingCollection<T>(new ConcurrentQueue<T>());
		public BlockingCollection<T> MessageQueue
		{
			get
			{
				return _MessageQueue;
			}
		}


		#region MessageListener Members

		public void PushMessage(T message)
		{
			_MessageQueue.Add(message);
		}

		#endregion

		#region IDisposable Members

		CancellationTokenSource cancellationSource = new CancellationTokenSource();
		public void Dispose()
		{
			if(cancellationSource.IsCancellationRequested)
				return;
			cancellationSource.Cancel();
		}

		#endregion

	}
}

