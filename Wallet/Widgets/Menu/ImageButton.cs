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

		public String Background {
			set {
				try {
			//		image.Pixbuf = Gdk.Pixbuf.LoadFromResource (value);
				} catch {
					Console.WriteLine ("missing" + value);
				}
			}
		}
	}
}

