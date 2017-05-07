//using System;
//namespace Infrastructure.TestingGtk
//{
//	public class ExcetionHandler
//	{
//		private Exception TraceException { get; set; }
//	//	private IRe / IShowMessageWindow
//		public ExcetionHandler()
//		{
//		}

//		public void Handler(Exception e, T resourceOwnerWindow = null)
//		{
//			Console.WriteLine(e);

//			try
//			{
//				Trace.Error("App", e);
//			}
//			catch (Exception traceExeption)
//			{
//				Console.WriteLine(traceExeption);
//			}

//			if (resourceOwnerWindow != null)
//			{
//				try
//				{
//					resourceOwnerWindow.ShowMessage($"App excption: {e}");
//				}
//				catch (Exception showException)
//				{
//					Console.WriteLine(showException);
//				}
//			}
//		}
//	}
//}
