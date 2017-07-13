using System;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;

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
			try
			{
				processMessage(message);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Exception during message handler", ex); //TODO: write to trace
				throw ex;
			}
		}

		#endregion
	}

	public class Wrapper<T>
	{
		public T Value { get; set; }

#if DEBUG
		public StackTrace StackTrace { get; set; }
#endif

		public Wrapper(T t)
		{
			Value = t;

#if DEBUG
			StackTrace = new StackTrace(true);
#endif
		}

	}

	public class EventLoopMessageListener<T> : IMessageListener<T>, IDisposable
	{
		private ManualResetEvent continueEvent = new ManualResetEvent(true);
		private Thread thread;

#if DEBUG
		public StackTrace _CreatorStackTrace;
#endif

		public EventLoopMessageListener(Action<T> processMessage, string threadName = null)
		{
#if DEBUG
			_CreatorStackTrace = new StackTrace(true);
#endif

			thread = new Thread(new ThreadStart(() =>
			{
				try
				{
					while (!cancellationSource.IsCancellationRequested)
					{
						Wrapper<T> message;

						message = _MessageQueue.Take(cancellationSource.Token);

						continueEvent.WaitOne();

						if (message != null)
						{
							try
							{
								//InfrastructureTrace.Information("processMessage: " + message.GetType());
								processMessage(message.Value);
							}
							catch (Exception ex)
							{
								InfrastructureTrace.Error("Exception during message loop", ex);
								Console.WriteLine("Exception during message loop", ex); //TODO: write to trace
																						//NodeServerTrace.Error("Exception during message loop", ex);

								throw ex;
							}
						}
					}
				}
				catch (OperationCanceledException)
				{
				}
			}));

			thread.Name = threadName ?? "EventLoopMessageHandler";
			thread.Start();
		}

		public void Pause()
		{
			continueEvent.Reset();
		}

		public void Continue()
		{
			continueEvent.Set();
		}

		BlockingCollection<Wrapper<T>> _MessageQueue = new BlockingCollection<Wrapper<T>>(new ConcurrentQueue<Wrapper<T>>());
		public BlockingCollection<Wrapper<T>> MessageQueue
		{
			get
			{
				return _MessageQueue;
			}
		}


		#region MessageListener Members

		public void PushMessage(T message)
		{
			_MessageQueue.Add(new Wrapper<T>(message));
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

