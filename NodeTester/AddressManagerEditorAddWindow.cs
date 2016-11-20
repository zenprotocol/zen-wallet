using System;
using System.Net;
using Gtk;

namespace NodeTester
{
	public partial class AddressManagerEditorAddWindow : Gtk.Window
	{
		System.Action<IPEndPoint> action;

		public AddressManagerEditorAddWindow (System.Action<IPEndPoint> action) :
			base (Gtk.WindowType.Toplevel)
		{
			this.Build ();

			this.action = action;
		}

		protected void Button_Cancel (object sender, EventArgs e)
		{
			Destroy ();
		}

		protected void Button_Add (object sender, EventArgs e)
		{
			IPAddress IPAddress = null;
			IPEndPoint IPEndPoint = null;
			int port;

			try {
				IPAddress = System.Net.IPAddress.Parse(entryAddress.Text);
			} catch
			{
				MessageDialog md = new MessageDialog(this, 
					DialogFlags.DestroyWithParent, MessageType.Error, 
					ButtonsType.Close, "Invalid IP Address");
				md.Run();
				md.Destroy();

				return;
			}

			try {
				port = int.Parse(entryPort.Text);
			} catch
			{
				MessageDialog md = new MessageDialog(this, 
					DialogFlags.DestroyWithParent, MessageType.Error, 
					ButtonsType.Close, "Invalid Port");
				md.Run();
				md.Destroy();

				return;
			}
				
			try {
				IPEndPoint = new IPEndPoint (IPAddress, port);
			} catch {
				MessageDialog md = new MessageDialog(this, 
					DialogFlags.DestroyWithParent, MessageType.Error, 
					ButtonsType.Close, "Invalid IPAddress settings");
				md.Run();
				md.Destroy();

				return;
			}


			try {
				action(IPEndPoint);
			} catch (Exception e_) {
				MessageDialog md = new MessageDialog(this, 
					DialogFlags.DestroyWithParent, MessageType.Error, 
					ButtonsType.Close, "Program error: " + e_.Message);
				md.Run();
				md.Destroy();

				return;
			}

			Destroy ();
		}
	}
}

