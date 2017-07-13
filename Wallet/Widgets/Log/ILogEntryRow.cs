using System;

namespace Wallet
{
	public interface ILogEntryRow
	{
        byte[] Key { get; }
		Object[] Values { get; } 
		int Offset { get; } 
	}
}

