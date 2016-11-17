using System;
using System.Diagnostics;
using System.Text;

namespace BlockChain.Database
{
	public static class DatabaseTrace
	{
		static TraceSource _Trace = new TraceSource("BlockChain.Database");

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

		internal static void Read(string tableName, Object key, Object value = null)
		{
			if (value != null)
				Information($"RD {tableName}, key: {KeyToString(key)}, value: {value}");
			else
				Information($"RD {tableName}, key: {KeyToString(key)}");
		}

		internal static void Write(string tableName, Object key, Object value = null)
		{
			if (value != null)
				Information($"WR {tableName}, key: {KeyToString(key)}, value: {value}");
			else
				Information($"WR {tableName}, key: {KeyToString(key)}");
		}

		internal static void KeyLookup(string tableName, Object key)
		{
			Information($"KY {tableName}, key: {KeyToString(key)}");
		}

		private static String KeyToString(Object key)
		{
			if (key is byte[])
				return BitConverter.ToString(key as byte[]).Substring(0,10);

			return key.ToString();
		}

	}
}