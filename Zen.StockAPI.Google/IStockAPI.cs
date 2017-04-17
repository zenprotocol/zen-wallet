using System;
using System.Collections.Generic;

namespace Zen.StockAPI.Google
{
	public interface StockAPI
	{
		string[] Tickers { get; }
		Dictionary<string, decimal> FetchResults();
	}
}
