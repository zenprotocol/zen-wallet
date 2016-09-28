using System;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class ImageButton : Gtk.Bin
	{
		public ImageButton ()
		{
			this.Build ();
		}

		public void SetBackground(String value) {
			image73.Pixbuf = Gdk.Pixbuf.LoadFromResource(value);
		}
	}
}

