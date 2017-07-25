using System;
using System.Diagnostics;

namespace Infrastructure
{
	public static class InfrastructureTrace
	{
		static TraceSource _Trace = new TraceSource("Infrastructure");

		public static void Information(string info)
		{
			_Trace.TraceEvent(TraceEventType.Information, 0, info);
		}

		public static void Warning(string info)
		{
			_Trace.TraceEvent(TraceEventType.Warning, 0, info);
		}

		public static void Error(string msg, Exception ex)
		{
			_Trace.TraceEvent(TraceEventType.Error, 0, msg + " " + Utils.ExceptionToString(ex));
		}

		public static void Verbose(string str)
		{
			_Trace.TraceEvent(TraceEventType.Verbose, 0, str);
		}
	}
}