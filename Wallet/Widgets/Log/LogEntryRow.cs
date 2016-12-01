using System;
using Wallet.Domain;

namespace Wallet
{
	public class LogEntryRow : ILogEntryRow
	{
		public Object[] Values { get; private set; } 
		public int Offset { get { return 0; }}

		public LogEntryRow (LogEntryItem logEntryItem) {
			Values = new System.Object[] {
				logEntryItem.Date, 
				logEntryItem.Id, 
				logEntryItem.Direction == DirectionEnum.Sent ? logEntryItem.Amount : 0,
				logEntryItem.Direction == DirectionEnum.Recieved ? logEntryItem.Amount : 0,
				logEntryItem.Balance
			};
		}
	}
}

