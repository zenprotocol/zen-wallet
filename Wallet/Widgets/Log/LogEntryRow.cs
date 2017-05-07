using System;
using Wallet.Domain;

namespace Wallet
{
	public class LogEntryRow : ILogEntryRow
	{
		public Object[] Values { get; private set; } 
		public int Offset { get { return 0; }}

		public LogEntryRow (LogEntryItem logEntryItem) {
			var zenAmount = new Zen(logEntryItem.Amount).ToString();
			Values = new System.Object[] {
				logEntryItem.Date.TimeAgo(), 
				logEntryItem.Id, 
				logEntryItem.Direction == DirectionEnum.Sent ? zenAmount : "0",
				logEntryItem.Direction == DirectionEnum.Recieved ? zenAmount : "0",
				logEntryItem.Balance
			};
		}
	}
}

