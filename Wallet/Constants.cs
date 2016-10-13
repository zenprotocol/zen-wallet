using System;

namespace Wallet.Constants
{
	public class Images
	{
		private static String resourceBase = "Wallet.Assets.";
		public static String UpArrow = resourceBase + "misc.arrowup.png";
		public static String DownArrow = resourceBase + "misc.arrowdown.png";
		public static String Send = resourceBase + "misc.send.png";
		public static String SendDialog = resourceBase + "misc.send_dialog.png";
		public static String Button(String name, bool selected) { return "Wallet.Assets." + name + (selected ? "_on.png" : "_off.png"); }
		public static String AssetLogo(String key) { return "Wallet.Assets.misc." + key + ".png"; }
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
		public static Pango.FontDescription LogHeader = Pango.FontDescription.FromString ("Aharoni CLM 14");
		public static Pango.FontDescription LogText = Pango.FontDescription.FromString ("Aharoni CLM 12");
		public static Pango.FontDescription DialogHeader = Pango.FontDescription.FromString ("Aharoni CLM 20");
		public static Pango.FontDescription DialogContent = Pango.FontDescription.FromString ("Aharoni CLM 12");
		public static Pango.FontDescription DialogContentBold = Pango.FontDescription.FromString ("Aharoni CLM 12");

		static Fonts() {
			DialogContentBold.Weight = Pango.Weight.Bold;
		}
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

		public static String Date = "Date";
		public static String Balance = "Balance";
		public static String TransactionId = "Transaction ID";
		public static String TotalReceived = "Total Received";
		public static String TotalBalance = "Total Balance";
		public static String TotalSent = "Total Sent";
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

	public class Formats {
		public static String Money = "{0:0.00#####}";
		public static String Date = "{0:G}";
	}
}