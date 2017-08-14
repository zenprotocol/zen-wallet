using System;
using Wallet.Domain;

namespace Wallet
{
	public class LogEntryRow : ILogEntryRow
	{
        public byte[] Key { get; private set; }
		public Object[] Values { get; private set; } 
        public LogEntryItem LogEntryItem { get; set; }


        public LogEntryRow (byte[] key, LogEntryItem logEntryItem) {
            Key = key;
            LogEntryItem = logEntryItem;
			//Values = new System.Object[] {
			//	logEntryItem.Date.TimeAgo(), 
			//	logEntryItem.Id, 
			//	logEntryItem.Direction == DirectionEnum.Sent ? logEntryItem.Amount : 0,
			//	logEntryItem.Direction == DirectionEnum.Recieved ? logEntryItem.Amount : 0,
			//	logEntryItem.Balance
			//};
		}
	}
}

