using System;
using Wallet.Domain;

namespace Wallet
{
	public class LogSummaryRow : ILogEntryRow
	{
        public byte[] Key { get { return null; }}
		public Object[] Values { get; private set; } 
		public int Offset { get { return 2; }}

		public LogSummaryRow (params Decimal[] values)
		{
			Values = Array.ConvertAll(values, item => (Object)item);
		}

		public Decimal this[int i]
		{
			get
			{
				return (Decimal)Values [i];
			}
			set
			{
				Values[i] = value;
			}
		}
	}
}

