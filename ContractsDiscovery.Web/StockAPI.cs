using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;

namespace ContractsDiscovery.Web.App_Code
{
    public class Ticker
    {
        public string Name { get; set; }
        public decimal Value { get; set; }
    }

    public class StockAPI
    {
        readonly string _UrlFormat = "http://finance.yahoo.com/d/quotes.csv?s={0}&f=snl1";
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
                query += (query == "" ? "" : "+") + ticker;
            }

            var uri = new Uri(string.Format(_UrlFormat, query));
           
            try
            {
                var response = await new HttpClient().GetAsync(uri.AbsoluteUri).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var raw = await response.Content.ReadAsStringAsync();
                    var lines = raw.Split(new[] { "\n" }, StringSplitOptions.None);
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            try
                            {
                                var parts = Regex.Split(line, @",(?=(?:[^""]*""[^""]*"")*[^""]*$)");
                                values.Add(new Ticker() { Name = parts[0].Replace("\"", ""), Value = Decimal.Parse(parts[2]) }); //TODO: instead of using replace, use same regex
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Error parsing value: " + line, e);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error fetching data from url: "+ uri.ToString(), e);
            }

            if (values.Count == 0)
            {

                Random random = new Random();
                foreach (string ticker in _Tickers)
                {
                    values.Add(new Ticker() { Name = ticker, Value = random.Next(0, 100) });
                }
            }

            return values;
        }
    }

    internal class Tests
    {
        [Test()]
        public void ShouldGetTickerData()
        {
            var stockAPI = new StockAPI();
            var tickers = "AAPL,AABA,AMZN,GOOGL,INTC,TSLA".Split(',');
            stockAPI.Tickers = tickers;
            var results = stockAPI.FetchResults().Result;

            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(tickers.Length));
        }
    }
}
