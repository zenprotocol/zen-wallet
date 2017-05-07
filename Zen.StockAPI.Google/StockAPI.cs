using System;
namespace Zen.StockAPI.Google
{
	public class MyClass : IStockAPI
	{
		readonly HttpClient _HttpClient = new HttpClient();

		public MyClass()
		{
		}
	}
}
