using System;
using Gdk;

namespace Wallet.Constants
{
	public class Images
	{
		private static String resourceBase = "Wallet.Assets.";
		public static String UpArrow = resourceBase + "misc.arrowup.png";
		public static String DownArrow = resourceBase + "misc.arrowdown.png";
	}

	public class Colors
	{
		public static Color Text = new Gdk.Color (0x0ff, 0x0ff, 0x0ff);
		public static Color Base = new Gdk.Color (0x024, 0x030, 0x03e);
	}
}