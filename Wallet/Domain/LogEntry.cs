using System;

namespace Wallet.Domain
{
	public class LogEntryItem
	{
		public Decimal Amount { get; set; }
		public CurrencyEnum Currency { get; set; }
		public DirectionEnum Direction { get; set; }
		public DateTime Date { get; set; }
		public String To { get; set; }
		public String Id { get; set; }
		public Decimal Balance { get; set; }

		public LogEntryItem(Decimal Amount, DirectionEnum Direction, CurrencyEnum Currency, DateTime Date, String To, String Id, Decimal Balance) {
			this.Amount = Amount;
			this.Direction = Direction;
			this.Currency = Currency;
			this.Date = Date;
			this.To = To;
			this.Id = Id;
			this.Balance = Balance;
		}
	}
}