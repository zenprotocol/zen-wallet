using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using NetMQ.Sockets;
using NetMQ;
using Consensus;
using Microsoft.FSharp.Collections;
using Zen.RPC.Common;
using Datafeed.Web.Models;
using Zen.RPC;
using System.Web.Configuration;

namespace Datafeed.Web.Controllers
{
    public class HomeController : Controller
    {
		StockAPI _StockAPI = new StockAPI();
		string address = WebConfigurationManager.AppSettings["node"];

		public ActionResult Index()
		{
			var model = Directory.GetFiles(".", "*.json").Select(t =>
            {
                try
                {
                    var time = Regex.Replace(t, @"[^\d]", "");
                    var datetime = DateTime.FromFileTime(long.Parse(time));
                    var data = System.IO.File.ReadAllText(t);
					var json = JsonConvert.DeserializeObject<Commitment>(data);

					return new App_Data.Commitment()
                    {
                        Id = time,
                        Time = datetime.ToLongDateString() + " " + datetime.ToLongTimeString(),
                        Count = json.items.Count(),
                        Data = Convert.ToBase64String(json.merkelRoot)
                    };
                } catch (Exception e)
                {
					return new App_Data.Commitment()
					{
						Id = "Parse error"
					};
				}
            });

			return View(model);
		}

		[HttpPost]
		public async Task<ActionResult> Generate()
		{
			_StockAPI.Tickers = new string[] { "GOOG", "GOOGL", "YHOO", "TSLA", "INTC", "AMZN" }; //, "APPL" };

			var model = await _StockAPI.FetchResults();
			var merkelRoot = GetMerkelRoot(model);

			var commitment = new Models.Commitment() { merkelRoot = merkelRoot, items = model };

			System.IO.File.WriteAllText($"{DateTime.Now.ToFileTime()}.json", JsonConvert.SerializeObject(commitment));

			var acsResult = await Client.Send<GetACSResultPayload>(address, new GetACSPayload());

			if (!acsResult.Success)
			{
				ViewData["Result"] = false;
				ViewData["Message"] = "Could not get active contracts list";
				return View();
			}

			byte[] contractHash = null;
			foreach (var acs in acsResult.Contracts)
			{
				try
				{
					var code = Encoding.ASCII.GetString(acs.Code);
					var header = code.Split(Environment.NewLine.ToCharArray())[0].Substring(2).Trim();

					dynamic headerJson = JsonConvert.DeserializeObject(header);

					if (headerJson.type == "oracle-datafeed")
					{
						contractHash = acs.Hash;
					}
				}
				catch
				{
				}
			}

			var getOutputResult = await Client.Send<GetOutpointResultPayload>(address, new GetOutpointPayload()
			{
				Asset = Tests.zhash,
				IsContract = true,
				Address = contractHash
			});

			if (!getOutputResult.Success)
			{
				ViewData["Result"] = false;
				ViewData["Message"] = "Expected outpoint missing";
				return View();
			}

            var data = merkelRoot.Concat(new byte[] { (byte)getOutputResult.Index });
            data = data.Concat(getOutputResult.TXHash);

			if (contractHash == null)
			{
				ViewData["Result"] = false;
				ViewData["Message"] = "Could not find contract";
			}
			else
			{
				var result = await Client.Send<ResultPayload>(address, new SendContractPayload()
				{
					ContractHash = contractHash,
                    Data = data.ToArray()
                });

				ViewData["Result"] = result.Success;
                ViewData["Message"] = "Resson: send result was '" + result.Message + "'";
			}

			return View();
		}

		private byte[] GetMerkelRoot(List<Ticker> tickers)
		{
			return Merkle.merkleRoot<byte[]>(
				new byte[] { },
				Merkle.hashHasher,
				ListModule.OfSeq(tickers.Select(t => Encoding.ASCII.GetBytes(t.Name + ';' + t.Value.ToString())))
			);
		}
    }
}
