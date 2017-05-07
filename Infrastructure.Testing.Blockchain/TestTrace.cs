using System;
using System.Diagnostics;
using System.Text;

namespace Infrastructure.Testing.Blockchain
{
	public static class TestTrace
	{
		static TraceSource _Trace = new TraceSource("TestTrace");

		internal static void Information(string info)
		{
			_Trace.TraceEvent(TraceEventType.Information, 0, info);
		}

		internal static void Error(string msg, Exception ex)
		{
			_Trace.TraceEvent(TraceEventType.Error, 0, msg + " " + ex.Message);
		}

		internal static void Verbose(string str)
		{
			_Trace.TraceEvent(TraceEventType.Verbose, 0, str);
		}

		internal static void Transaction(string tag, Object key)
		{
			Information($"Transaction {tag}, key: {KeyToString(key)}");
		}

		private static String KeyToString(Object key)
		{
			String val;

			if (key is byte[])
				val = System.Convert.ToBase64String(key as byte[]);
			else 
				val = key.ToString();

			if (val.Length > 11)
				val = val.Substring(0, 11);

			return val;
		}

	}
}