using System;
using Wallet.Domain;

namespace Wallet
{
	public class LogHeaderRow : ILogEntryRow
	{
		public byte[] Key { get { return null; } }
		public Object[] Values { get; private set; } 
		public int Offset { get; private set; } 

		public LogHeaderRow(int offset, params String[] values)
		{
			Offset = offset;
			Values = Array.ConvertAll(values, item => (Object)item);
		}
	}
}

