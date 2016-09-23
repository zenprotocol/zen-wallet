using System;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class WidgetButtonContent : Gtk.Bin
	{
		public WidgetButtonContent ()
		{
			this.Build ();
		}

		public void SetBackground(String value) {
			image73.Pixbuf = Gdk.Pixbuf.LoadFromResource(value);
		}
	}
}

