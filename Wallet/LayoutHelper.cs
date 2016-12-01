using System;
using Wallet.Constants;

namespace Wallet
{
	public class LayoutHelper
	{	
		public String Text { get; private set; }
		public Pango.Alignment Alignment { get; private set; }

		private LayoutHelper(String text, Pango.Alignment alignment) {
			Text = text;
			Alignment = alignment;
		}

		public static LayoutHelper Factor(Object value)
		{
			if (value.GetType () == typeof(String)) {
				return new LayoutHelper(value.ToString (), Pango.Alignment.Right);
			} if (value.GetType () == typeof(Decimal) || value.GetType () == typeof(Double) || value.GetType () == typeof(int)) {
				if (value.ToString() != "0") {
					return new LayoutHelper(String.Format(Formats.Money, value), Pango.Alignment.Right);
				} else {
					return new LayoutHelper("", Pango.Alignment.Right);
				}
			} if (value.GetType () == typeof(DateTime)) {
				return new LayoutHelper(String.Format (Formats.Date, value), Pango.Alignment.Right);
			}

			throw new Exception ();
		}
	}
}