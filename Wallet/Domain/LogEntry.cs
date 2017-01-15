using System;
using Wallet.core;

namespace Wallet.Domain
{
	public class LogEntryItem
	{
		public Decimal Amount { get; set; }
		public AssetType Asset { get; set; }
		public DirectionEnum Direction { get; set; }
		public DateTime Date { get; set; }
		public String To { get; set; }
		public String Id { get; set; }
		public Decimal Balance { get; set; }

		public LogEntryItem(Decimal Amount, DirectionEnum Direction, AssetType Asset, DateTime Date, String To, String Id, Decimal Balance) {
			this.Amount = Amount;
			this.Direction = Direction;
			this.Asset = Asset;
			this.Date = Date;
			this.To = To;
			this.Id = Id;
			this.Balance = Balance;
		}
	}
}