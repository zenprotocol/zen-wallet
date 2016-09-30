using System;

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
		public static Color Text = new Color (0x0ff, 0x0ff, 0x0ff);
		public static Color Text2 = new Color(0x0f7, 0x0f7, 0x0f7);
		public static Color SubText = new Color (0x0af, 0x0af, 0x0af);
	//	public static Color SubText2 = new Color(0x0c7, 0x0c7, 0x0c7);
		public static Color Base = new Color (0x024, 0x030, 0x03e);
		public static Color ButtonUnselected = new Color(0x01d,0x025,0x030);
		public static Color ButtonSelected = new Color(0x028,0x02f,0x037);
	}

	public class Fonts
	{
		public static Pango.FontDescription ActionBarBig = Pango.FontDescription.FromString ("Aharoni CLM 30");
		public static Pango.FontDescription ActionBarSmall = Pango.FontDescription.FromString ("Aharoni CLM 15");
	}

	public class Strings
	{
		public static String Received = "Received";
		public static String Sent = "Sent";
		public static String DaysAgo(int days) { 
			switch (days) {
			case 0:
				return "today";
			case 1:
				return "yesterday";
			}
			return String.Format ("{0} days ago", days);
		}
		public static String MonthsAgo(int months) { 
			return String.Format ("{0} months ago", months);
		}
	}

	public class Color {
		byte r;
		byte g;
		byte b;

		public Color(byte r, byte g, byte b) {
			this.r = r;
			this.g = g;
			this.b = b;
		}

		public Cairo.Color Cairo {
			get {
				return new Cairo.Color (r, g, b);
			}
		}

		public Gdk.Color Gdk {
			get {
				return new Gdk.Color (r, g, b);
			}
		}

		public override string ToString ()
		{
			return string.Format("{0:X2}{1:X2}{2:X2}", r, g, b);
		}
	}
}