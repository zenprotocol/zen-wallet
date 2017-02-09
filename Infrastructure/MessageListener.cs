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

	public class EventLoopMessageListener<T> : IMessageListener<T>, IDisposable
	{
		private ManualResetEvent continueEvent = new ManualResetEvent(true);
		private Thread thread;

#if DEBUG
		public string _CreatorStackTrace;
#endif

		public EventLoopMessageListener(Action<T> processMessage)
		{
#if DEBUG
			_CreatorStackTrace = Environment.StackTrace;
#endif

			thread = new Thread(new ThreadStart(() =>
			{
				try
				{
					while (true)
					{
						T message;

						if (cancellationSource.IsCancellationRequested)
						{
							_MessageQueue.TryTake(out message);
						}
						else
						{
							message = _MessageQueue.Take(cancellationSource.Token);
						}

						continueEvent.WaitOne();

						if (message != null)
						{
							try
							{
								InfrastructureTrace.Information("processMessage: " + message.GetType());
								processMessage(message);
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

		//	thread.Join();
		}

		#endregion	

	}
}

