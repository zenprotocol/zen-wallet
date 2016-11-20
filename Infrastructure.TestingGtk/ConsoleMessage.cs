using System;
using System.IO;

namespace Infrastructure.TestingGtk
{
	public class ConsoleMessage
	{
		public String Text { get; set; }

		public static void Create(String Text) 
		{
			MessageProducer<ConsoleMessage>.Instance.PushMessage(new ConsoleMessage() { Text = Text });
		}

		public static TextWriter Out = new ConsoleWriter(
			v => ConsoleMessage.Create (v),
			v => ConsoleMessage.Create (v + "\n")
		);
	}
}

