using System;
using System.Diagnostics;

namespace Zen
{
	public class AppTraceListener : TraceListener
	{
		private static object _sync = new object();

		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
		{
			TraceEvent(eventCache, source, eventType, id, message, new object[0]);
		}

		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
		{
			lock (_sync)
			{
				if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null)) return;
				ConsoleColor color;
				switch (eventType)
				{
					case TraceEventType.Error:
						color = ConsoleColor.Red;
						break;
					case TraceEventType.Warning:
						color = ConsoleColor.Yellow;
						break;
					case TraceEventType.Information:
						color = ConsoleColor.Green;
						break;
					case TraceEventType.Verbose:
						color = ConsoleColor.DarkCyan;
						break;
					default:
						color = ConsoleColor.Gray;
						break;
				}

				var eventTypeString = Enum.GetName(typeof(TraceEventType), eventType);
				var message = source + " - " + eventTypeString + " > " + (args != null && args.Length > 0 ? string.Format(format, args) : format);

				TUI.WriteColor(message + Environment.NewLine, color);
			}
		}

		public override void Write(string message)
		{
			TUI.WriteColor(message, ConsoleColor.Gray);
		}
		public override void WriteLine(string message)
		{
			TUI.WriteColor(message + Environment.NewLine, ConsoleColor.Gray);
		}
	}
}
