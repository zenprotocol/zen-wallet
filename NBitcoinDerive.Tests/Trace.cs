using System;
using System.Diagnostics;

namespace NBitcoinDerive.Tests
{
	public static class Trace
	{
		static TraceSource _Trace = new TraceSource("Tests");

		internal static void Information(string info)
		{
			_Trace.TraceEvent(TraceEventType.Information, 0, info);
		}

		internal static void Error(string msg, Exception ex)
		{
			_Trace.TraceEvent(TraceEventType.Error, 0, msg + " " + Infrastructure.Utils.ExceptionToString(ex));
		}

		internal static void Verbose(string str)
		{
			_Trace.TraceEvent(TraceEventType.Verbose, 0, str);
		}

		internal static void WebClient(string str)
		{
			Verbose(str);
		}
	}
}