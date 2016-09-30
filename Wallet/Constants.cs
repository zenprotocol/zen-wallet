using System;
using Gdk;
using Pango;

namespace Wallet.Constants
{
	public class Images
	{
		private static String resourceBase = "Wallet.Assets.";
		public static String UpArrow = resourceBase + "misc.arrowup.png";
		public static String DownArrow = resourceBase + "misc.arrowdown.png";
		public static String Button(String name, bool selected) { return "Wallet.Assets." + name + (selected ? "_on.png" : "_off.png"); }
		public static String CurrencyLogo(String name) { return "Wallet.Assets.misc." + name + ".png"; }
	}

	public class Colors
	{
		public static Gdk.Color Text = new Gdk.Color (0x0ff, 0x0ff, 0x0ff);
		public static Gdk.Color Text2 = new Gdk.Color(0x0f7, 0x0f7, 0x0f7);
		public static Gdk.Color Base = new Gdk.Color (0x024, 0x030, 0x03e);
		public static Gdk.Color ButtonUnselected = new Gdk.Color(0x01d,0x025,0x030);
		public static Gdk.Color ButtonSelected = new Gdk.Color(0x028,0x02f,0x037);
	}

	public class Fonts
	{
		public static FontDescription ActionBarBig = Pango.FontDescription.FromString ("Aharoni CLM 30");
		public static FontDescription ActionBarSmall = Pango.FontDescription.FromString ("Aharoni CLM 15");
	}
}