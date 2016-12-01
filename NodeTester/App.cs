using System;
using Infrastructure;
using NodeCore;

namespace NodeTester
{
	public class App<T> where T : Gtk.Window, IResourceOwner, new()
	{
		static App() {
			JsonLoader<NodeCore.Settings>.Instance.FileName = "NodeTester.json";
			JsonLoader<Settings>.Instance.FileName = "NodeTester.json";
		}

		public static App<T> Create()
		{
			return new App<T>();
		}

		private App()
		{
			T t = null;
			TryCatch(() => { t = new T(); t.Show(); }, e => ExceptionHandler(e));

			if (JsonLoader<Settings>.Instance.Value.AutoConfigure)
			{
				TryCatch(t, w => Runtime.Instance.Configure(w), (e, w) => ExceptionHandler(e, w));
			}
		}

		private void TryCatch(Action TryAction, Action<Exception> CatchAction)
		{
			try
			{
				TryAction();
			}
			catch (Exception e)
			{
				CatchAction(e);
			}
		}

		private void TryCatch(T t, Action<T> TryAction, Action<Exception, T> CatchAction)
		{
			try {
				TryAction(t);
			} catch (Exception e) {
				CatchAction(e, t);
			}
		}

		private void ExceptionHandler (Exception e, T resourceOwnerWindow = null)
		{
			Console.WriteLine(e);

			try
			{
				Trace.Error("App", e);
			}
			catch (Exception traceExeption)
			{
				Console.WriteLine(traceExeption);
			}

			if (resourceOwnerWindow != null)
			{
				try
				{
					resourceOwnerWindow.ShowMessage($"App excption: {e.Message}");
				}
				catch (Exception showException)
				{
					Console.WriteLine(showException);
				}
			}
		}
	}
}