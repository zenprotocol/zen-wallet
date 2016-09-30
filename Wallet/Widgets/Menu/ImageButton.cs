using System;
using Gtk;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class ImageButton : WidgetBase
	{
		public ImageButton ()
		{
			this.Build ();
		}

		public void SetBackground(String value) {
			try {
				FindChild<Image>().Pixbuf = Gdk.Pixbuf.LoadFromResource(value);
			} catch {
				Console.WriteLine("missing" + value);
			}
		}
	}
}

