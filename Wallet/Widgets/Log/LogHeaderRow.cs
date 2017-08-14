using System;
using Wallet.Domain;

namespace Wallet
{
	public class LogHeaderRow : ILogEntryRow
	{
		public byte[] Key { get { return null; } }
		public Object[] Values { get; private set; } 

		public LogHeaderRow(params String[] values)
		{
			Values = Array.ConvertAll(values, item => (Object)item);
		}
	}
}

