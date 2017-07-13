using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using Consensus;
using ContractsDiscovery.Web.App_Data;
using Zen.RPC.Common;
using Zen.RPC;
using ContractsDiscovery.Web.App_Code;
using System.Web.Configuration;

namespace ContractsDiscovery.Web.Controllers
{
    public class OracleController : Controller
    {
		StockAPI _StockAPI = new StockAPI();
		static string NodeRPCAddress = WebConfigurationManager.AppSettings["node"];

    public ActionResult List()
		{
      return View();
    }

		public ActionResult Index()
		{
			 if (!Directory.Exists("db"))
			 	Directory.CreateDirectory("db");
      
			 var contractManager = new OracleContractManager();
      
			 if (!contractManager.IsSetup)
			 {
			 	ViewBag.Message = contractManager.Message;
			 	return View(new List<Commitment>());
			 }
      
			 ViewBag.Address = contractManager.ContractAddress;
      
			 return View(Directory.GetFiles("db", "*.data.json")
			 	.Select(t => Regex.Replace(t, @"[^\d]", ""))
			 	.OrderBy(t => t)
			 	.Reverse().Take(5).Reverse()
			 	.Select(t =>
			 	{
			 		var datetime = DateTime.FromFileTime(long.Parse(t));
			 		var file = Path.Combine("db", $"{t}.data.json");
      
			 		var data = System.IO.File.ReadAllText(file);
      
			 		return new App_Data.Commitment()
			 		{
			 			Id = t,
			 			Time = datetime.ToLongDateString() + " " + datetime.ToLongTimeString(),
			 		};
			 	}));
		}

		[HttpPost]
		public ActionResult Generate()
		{
			if (!Directory.Exists("db"))
				Directory.CreateDirectory("db");

			var contractManager = new OracleContractManager();

			if (!contractManager.IsSetup)
			{
				ViewData["Result"] = false;
				ViewData["Message"] = contractManager.Message;
				return View();
			}

			_StockAPI.Tickers = new string[] { "GOOG", "GOOGL", "YHOO", "TSLA", "INTC", "AMZN" }; //, "APPL" };

            var rawTickers = _StockAPI.FetchResults().Result;
			var now = DateTime.Now.ToUniversalTime();
			var nowTicks = now.Ticks;
			var items = rawTickers.Select(t => new ContractExamples.Oracle.TickerItem(t.Name, t.Value, nowTicks));
			var secret = Convert.FromBase64String(WebConfigurationManager.AppSettings["oracleSecret"]);
			var commitmentData = ContractExamples.Oracle.commitments(items, secret);

            var getOutpointsResult = Client.Send<GetContractPointedOutputsResultPayload>(NodeRPCAddress, new GetContractPointedOutputsPayload() { ContractHash = contractManager.ContractAddress.Bytes }).Result;

			if (!getOutpointsResult.Success || getOutpointsResult.PointedOutputs.Count == 0)
			{
				ViewData["Result"] = false;
				ViewData["Message"] = "Could not find outputs";
				return View();
			}

			var utxos = GetContractPointedOutputsResultPayload.Unpack(getOutpointsResult.PointedOutputs);

			if (utxos.Count() == 0)
			{
				ViewData["Result"] = false;
				ViewData["Message"] = "Could not find output";
				return View();
			}

			var utxo = utxos.First();

			var data = ContractExamples.QuotedContracts.simplePackOutpoint(utxo.Item1)
						   .Concat(commitmentData.Item2).ToArray();

			var signiture = Authentication.sign(data, contractManager.PrivateKey);

			data = data.Concat(signiture).ToArray();

			var sendContractResult = Client.Send<SendContractResultPayload>(NodeRPCAddress, new SendContractPayload()
			{
				ContractHash = contractManager.ContractAddress.Bytes,
				Data = data
            }).Result;

			ViewData["Result"] = sendContractResult.Success;

			if (sendContractResult.Success)
			{
				var file = Path.Combine("db", $"{now.ToFileTime()}");
				MemoryStream ms = new MemoryStream();
				ContractExamples.Oracle.proofMapSerializer.WriteObject(ms, commitmentData.Item1);
				ms.Position = 0;
				var sr = new StreamReader(ms);
				System.IO.File.WriteAllText(Path.ChangeExtension(file, ".data.json"), sr.ReadToEnd());

				var outpoint = new Types.Outpoint(sendContractResult.TxHash, 1); // oracle always puts data on output #1

				System.IO.File.WriteAllText(Path.ChangeExtension(file, ".outpoint.txt"), Convert.ToBase64String(Merkle.serialize(outpoint)));
			}
			else
			{
				ViewData["Message"] = "Resson: send result was '" + sendContractResult.Message + "'";
			}

			return View();
		}
    }
}
