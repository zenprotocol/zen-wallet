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
	}
}

