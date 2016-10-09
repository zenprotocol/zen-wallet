using System;

namespace Wallet
{
	public interface ILogEntryRow
	{
		Object[] Values { get; } 
		int Offset { get; } 
	}
}

