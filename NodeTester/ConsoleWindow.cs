using System;
using Gtk;
using Infrastructure;
using Infrastructure.TestingGtk;

namespace NodeTester
{
	public partial class ConsoleWindow : Window
	{
		public System.Action OnSettingsClicked;

		public ConsoleWindow (IResourceOwner resourceOwner) :
			base (Gtk.WindowType.Toplevel)
		{
			this.Build ();

			textviewConsole.SizeAllocated += new SizeAllocatedHandler(ScrollToEnd);

			resourceOwner.OwnResource (MessageProducer<ConsoleMessage>.Instance.AddMessageListener (new EventLoopMessageListener<ConsoleMessage> (ConsoleMessage => {
				Gtk.Application.Invoke (delegate {
					textviewConsole.Buffer.Insert(textviewConsole.Buffer.EndIter, ConsoleMessage.Text);
				});
			})));

			buttonConsoleSettings.Clicked += (sender, e) =>
			{
				if (OnSettingsClicked != null)
				{
					OnSettingsClicked();
				}
			};
		}

		protected void OnDeleteEvent (object sender, DeleteEventArgs a)
		{
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

