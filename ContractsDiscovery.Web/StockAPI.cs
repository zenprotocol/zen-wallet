using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ContractsDiscovery.Web.App_Code
{
	public class Ticker
	{
		public string Name { get; set; }
		public decimal Value { get; set; }
	}

	class DataItem
	{
		public string t { get; set; }
		public decimal l { get; set; }
	}

	public class StockAPI
	{
		readonly string _Source = "NASDAQ";
		readonly string _UrlFormat = "http://finance.google.com/finance/info?q={0}";
		string[] _Tickers;

		public string[] Tickers
		{
			set
			{
				_Tickers = value;
			}
		}

		public async Task<List<Ticker>> FetchResults()
		{
            var values = new List<Ticker>();
            var query = "";

            foreach (var ticker in _Tickers)
            {
                query += (query == "" ? "" : ",") + string.Format("{0}:{1}", _Source, ticker);

                var uri = new Uri(string.Format(_UrlFormat, query));

                try
                {
					var response = await new HttpClient().GetAsync(uri.AbsoluteUri).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        var raw = await response.Content.ReadAsStringAsync();
                        raw = raw.Replace("//", "");
                        var json = JsonConvert.DeserializeObject<List<DataItem>>(raw);

                        foreach (var dataItem in json)
                        {
                            values.Add(new Ticker() { Name = dataItem.t, Value = dataItem.l });
                        }
                    }
                }
                catch
                {

                }
            }

            return values;
		}
	}
}
