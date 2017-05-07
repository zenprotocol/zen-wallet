using System;

namespace Wallet
{
	public class Utils
	{
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

