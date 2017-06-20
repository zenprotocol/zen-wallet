using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using Consensus;
using Datafeed.Web.App_Data;
using Zen.RPC.Common;
using Zen.RPC;
using Datafeed.Web.App_Code;

namespace Datafeed.Web.Controllers
{
    public class HomeController : Controller
    {
		StockAPI _StockAPI = new StockAPI();

		public ActionResult Index()
		{
			byte[] contractHash;
			byte[] privateKey;

			if (!Utils.EnsureFunds())
            {
                ViewBag.Message = "No funds";
                return View(new List<Commitment>());
            }
            else if (!Utils.EnsureOracle(out contractHash, out privateKey))
            {
                ViewBag.Message = "Could not setup the contract";
                return View(new List<Commitment>());
            }
            else
            {
                ViewBag.Address = new Wallet.core.Data.Address(contractHash, Wallet.core.Data.AddressType.Contract);

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
		}

		[HttpPost]
		public async Task<ActionResult> Generate()
		{
			byte[] contractHash;
			byte[] privateKey;

			if (!Utils.EnsureOracle(out contractHash, out privateKey))
			{
				ViewData["Result"] = false;
				ViewData["Message"] = "Could not setup the contract";
				return View();
			}

			_StockAPI.Tickers = new string[] { "GOOG", "GOOGL", "YHOO", "TSLA", "INTC", "AMZN" }; //, "APPL" };

			var rawTickers = await _StockAPI.FetchResults();
            var now = DateTime.Now.ToUniversalTime();
            var nowTicks = now.Ticks;
            var items = rawTickers.Select(t => new ContractExamples.Oracle.TickerItem(t.Name, t.Value, nowTicks));
            var secret = new byte[] { 0x00, 0x01, 0x02 }; // System. WebConfigurationManager.AppSettings["secret"];
            var commitmentData = ContractExamples.Oracle.commitments(items, secret);

            var getOutpointsResult = await Client.Send<GetContractPointedOutputsResultPayload>(Utils.NodeRPCAddress, new GetContractPointedOutputsPayload() { ContractHash = contractHash });

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

            var signiture = Authentication.sign(data, privateKey);

            data = data.Concat(signiture).ToArray();

            var sendContractResult = await Client.Send<SendContractResultPayload>(Utils.NodeRPCAddress, new SendContractPayload()
            {
                ContractHash = contractHash,
                Data = data
            });

            ViewData["Result"] = sendContractResult.Success;

            if (sendContractResult.Success)
            {
                if (!Directory.Exists("db"))
                    Directory.CreateDirectory("db");

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
