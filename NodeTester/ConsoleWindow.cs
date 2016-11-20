using System;
using Gtk;
using Infrastructure;
using Infrastructure.TestingGtk;

namespace NodeTester
{
	public partial class ConsoleWindow : ResourceOwnerWindow
	{
		public ConsoleWindow () :
			base (Gtk.WindowType.Toplevel)
		{
			this.Build ();

			textviewConsole.SizeAllocated += new SizeAllocatedHandler(ScrollToEnd);


			OwnResource (MessageProducer<ConsoleMessage>.Instance.AddMessageListener (new EventLoopMessageListener<ConsoleMessage> (ConsoleMessage => {
				Gtk.Application.Invoke (delegate {
					textviewConsole.Buffer.Insert(textviewConsole.Buffer.EndIter, ConsoleMessage.Text);
				});
			})));
		}

		protected void OnDeleteEvent (object sender, DeleteEventArgs a)
		{
			DisposeResources ();
		}

		protected void Button_Clear (object sender, EventArgs e)
		{
			textviewConsole.Buffer.Clear ();
		}

		private void ScrollToEnd(object sender, Gtk.SizeAllocatedArgs e)
		{
			textviewConsole.ScrollToIter(textviewConsole.Buffer.EndIter, 0, false, 0, 0);
		}
	}
}

