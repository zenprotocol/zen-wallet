using System;

namespace Wallet
{
	public class Utils
	{
		public static Decimal ToDecimal(String value) {
			Decimal result;

			Decimal.TryParse (value, out result);

			return result;
		}

		public static Gdk.Pixbuf ToPixbuf(String resourceName) {
			try {
				return Gdk.Pixbuf.LoadFromResource(resourceName);
			} catch {
				Console.WriteLine ("missing resource: " + resourceName);
			}

			return null;
		}
	}
}

