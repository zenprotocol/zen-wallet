using System;
using System.IO;
using System.Text;

namespace Infrastructure.TestingGtk
{
	public class ConsoleWriterEventArgs : EventArgs
	{
		public string Value { get; private set; }
		public ConsoleWriterEventArgs(string value)
		{
			Value = value;
		}
	}

	public class ConsoleWriter : TextWriter
	{
		public override Encoding Encoding { get { return Encoding.UTF8; } }

		public override void Write(string value)
		{
			if (WriteEvent != null)
				WriteEvent(this, new ConsoleWriterEventArgs(value));
			base.Write(value);
		}

		public override void WriteLine(string value)
		{
			if (WriteLineEvent != null) 
				WriteLineEvent(this, new ConsoleWriterEventArgs(value));
			base.WriteLine(value);
		}

		public event EventHandler<ConsoleWriterEventArgs> WriteEvent;
		public event EventHandler<ConsoleWriterEventArgs> WriteLineEvent;

		Action<String> WriteAction;
		Action<String> WriteLineAction;

		static TextWriter Console;

		public ConsoleWriter(Action<String> WriteAction, Action<String> WriteLineAction)
		{
			this.WriteAction = WriteAction;
			this.WriteLineAction = WriteLineAction;

			WriteEvent += console_WriteEvent;
			WriteEvent += consoleWriter_WriteEvent;
			WriteLineEvent += console_WriteLineEvent;
			WriteLineEvent += consoleWriter_WriteLineEvent;

			Console = System.Console.Out;
			System.Console.SetOut(this);
		}

		//todo: on dispose - remove event hooks
			
		void consoleWriter_WriteLineEvent(object sender, ConsoleWriterEventArgs e)
		{
			WriteAction(e.Value + "\n");
		}
			
		void consoleWriter_WriteEvent(object sender, ConsoleWriterEventArgs e)
		{
			WriteAction(e.Value);
		}

		void console_WriteLineEvent(object sender, ConsoleWriterEventArgs e)
		{
			Console.WriteLine (e.Value);
		}
			
		void console_WriteEvent(object sender, ConsoleWriterEventArgs e)
		{
			Console.Write (e.Value);
		}
	}
}

