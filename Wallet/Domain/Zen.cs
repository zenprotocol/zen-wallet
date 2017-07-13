using System;
using Wallet.Constants;

namespace Wallet
{
	public class Zen
	{
		readonly decimal _Exp = (decimal)Math.Pow(10, 8);

		public ulong Kalapas { get; set; }

		public Zen(string text)
		{
			Text = text;
		}

		public Zen(long value) : this((ulong)Math.Abs(value))
		{
		}

		public Zen(ulong value)
		{
			Kalapas = value;
		}

		public decimal Value
		{
			get
			{
				return Kalapas / _Exp;
			}
			set
			{
				Kalapas = (ulong) (value * _Exp);
			}
		}

		public string Text
		{
			set
			{
				Value = decimal.Parse(value.Trim());
			}
		}

		public override string ToString()
		{
			return String.Format(Formats.Money, Value);
		}

		public static bool IsValidText(string text, out Zen zen)
		{
			var parts = text.Trim().Split('.');
            zen = null;

			if (parts.Length == 2 && parts[1].Length > 8)
			{
				return false;
			}

			long kalapas;
			var isValid = long.TryParse(text, out kalapas);

            if (isValid)
                zen = new Zen(kalapas);

            return isValid;
		}
	}
}
