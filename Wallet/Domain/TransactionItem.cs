using System;

namespace Wallet.Domain
{
	public enum CurrencyEnum {
		BTC, ETH, ZEN, Lite
	}

	public enum DirectionEnum {
		Sent, Recieved
	}

	public class TransactionItem
	{
		public Decimal Amount { get; set; }
		public CurrencyEnum Currency { get; set; }
		public DirectionEnum Direction { get; set; }

		public TransactionItem(Decimal Amount, DirectionEnum Direction /*, CurrencyEnum Currency*/) {
			this.Amount = Amount;
			this.Direction = Direction;
		}
	}
}

