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
        public static String Sent = resourceBase + "misc.Sent.png";
		public static String Received = resourceBase + "misc.Received.png";
	}

    public class Colors
	{
        public static Color TextHeader = new Color (0x0ff, 0x0ff, 0x0ff);
		public static Color Text = new Color(0x083, 0x098, 0x0b6);
		public static Color Text2 = new Color(0x0f7, 0x0f7, 0x0f7);
		public static Color LabelText = new Color(0x0b8, 0x0c5, 0x0d8);
		public static Color TextBlue = new Color(0x083, 0x0d6, 0x0f3);
		public static Color SubText = new Color (0x0af, 0x0af, 0x0af);
	//	public static Color SubText2 = new Color(0x0c7, 0x0c7, 0x0c7);
		public static Color Base = new Color (0x01c, 0x01b, 0x033); // BG of everything
        public static Color ButtonUnselected = new Color(0x00e, 0x00e, 0x025); // BG of top menu - 2d2c47
		public static Color ButtonSelected = new Color(0x01c,0x01b,0x033); // BG of top menu - 3f3e5a
        public static Color LogAlternate = new Color(0x026, 0x026, 0x042);
		public static Color Textbox = new Color(0x034,0x047,0x05a);
        public static Color DialogBackground = Colors.ButtonSelected;
		public static Color Error = new Color(0x0ff, 0x04d, 0x04d);
		public static Color Success = new Color(0x00, 0x0ff, 0x00);
		public static Color Seperator = new Color(0x045, 0x04c, 0x064);
        internal static Color LogHeader = new Color(0x05c, 0x06b, 0x080);
		internal static Color LogReceived = new Color(0x01a, 0x0c5, 0x00d);
		internal static Color LogSent = new Color(0x0ff, 0x04d, 0x04d);
		internal static Color LogBox = new Color(0x02a, 0x029, 0x049);
	}

	public class Fonts
	{
		public static Pango.FontDescription ActionBarBig = Pango.FontDescription.FromString("Aharoni CLM 30");
		public static Pango.FontDescription LogBig = Pango.FontDescription.FromString("Aharoni CLM 30");
		public static Pango.FontDescription Balance = Pango.FontDescription.FromString("Aharoni CLM 30");
		public static Pango.FontDescription ActionBarSmall = Pango.FontDescription.FromString("Aharoni CLM 15");
		public static Pango.FontDescription ActionBarIntermediate  = Pango.FontDescription.FromString("Aharoni CLM 16");
		public static Pango.FontDescription LogHeader = Pango.FontDescription.FromString ("Aharoni CLM 14");
		public static Pango.FontDescription LogText = Pango.FontDescription.FromString ("Aharoni CLM 14");
		public static Pango.FontDescription DialogHeader = Pango.FontDescription.FromString ("Aharoni CLM 20");
		public static Pango.FontDescription DialogContent = Pango.FontDescription.FromString ("Aharoni CLM 12");
		public static Pango.FontDescription DialogContentBold = Pango.FontDescription.FromString ("Aharoni CLM 12");

		static Fonts() {
			DialogContentBold.Weight = Pango.Weight.Bold;
            ActionBarBig.Weight = Pango.Weight.Normal;
            LogBig.Weight = Pango.Weight.Light;
            Balance.Weight = Pango.Weight.Light;
		}
	}

	public class Strings
	{
		public static String Received = "Received";
		public static String Sent = "Sent";
		public static String Date = "Date";
		public static String Balance = "Balance";
		public static String TransactionId = "Status";
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