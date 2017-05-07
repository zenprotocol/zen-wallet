using System;
using Infrastructure;

namespace NodeTester
{
	class LogMessageContext : MessageProducer<LogMessage> {
		private String Tag { get; set; }

		public LogMessageContext(String Tag) 
		{
			this.Tag = Tag;
		}

		public void Create(String Text) 
		{
			Instance.PushMessage(new LogMessage() { Tag = Tag, Text = Text });
		}
	}

	class LogMessage
	{
		public String Tag { get; set; }
		public String Text { get; set; }
	}
}

