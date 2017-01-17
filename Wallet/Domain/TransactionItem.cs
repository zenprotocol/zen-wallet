using System;
using Wallet.core;

namespace Wallet.Domain
{
	public class TransactionItem
	{
		public ulong Amount { get; set; }
		public AssetType Asset { get; set; }
		public DirectionEnum Direction { get; set; }
		public DateTime Date { get; set; }
		public String To { get; set; }
		public String Id { get; set; }
		public Decimal Fee { get; set; }
		public TransactionItem PreviousTransactionItem { get; set; }
		//public Decimal RunningBalance { get 
		//	{ 
		//		return (Decimal)0.99;
		//	}
		//}

		public TransactionItem(ulong Amount, DirectionEnum Direction, AssetType Asset, DateTime Date, String To, String Id, Decimal Fee) {
			this.Amount = Amount;
			this.Direction = Direction;
			this.Asset = Asset;
			this.Date = Date;
			this.To = To;
			this.Id = Id;
			this.Fee = Fee;
		}
	}
}