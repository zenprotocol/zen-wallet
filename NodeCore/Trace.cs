using System;
using System.Diagnostics;

namespace NodeCore
{
	public static class Trace
	{
		static TraceSource _Trace = new TraceSource(System.Reflection.Assembly.GetExecutingAssembly().ToString().Split(',')[0]);

		internal static void Information(string info)
		{
			//TODO: this is not working: _Trace.TraceInformation(info);
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
	}
}