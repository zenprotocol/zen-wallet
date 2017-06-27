using System;
using System.Linq;
using System.Web.Mvc;
using System.IO;
using System.Text.RegularExpressions;
using Consensus;
using Microsoft.FSharp.Collections;
using Datafeed.Web.App_Data;
using Datafeed.Web.App_Code;

namespace Datafeed.Web.Controllers
{
	public class DataController : Controller
	{
		public ActionResult Index()
		{
			return View();
		}

		public JsonResult Status() { 
			var contractManager = new ContractManager();
			dynamic json;

			if (!contractManager.IsSetup)
			{
				json = new { isSetup = false, message = contractManager.Message };
			}
			else
			{
				json = new { isSetup = true, address = contractManager.ContractAddress.ToString() };
			}

			return Json(json, JsonRequestBehavior.AllowGet);
		}

		public ActionResult ShowTicker(string ticker)
		{
			DateTime dateTime;
			decimal value;

			if (GetLastValue(ticker, out value, out dateTime))
			{
				return View("Search", new CommitmentData()
				{
					Ticker = ticker,
					Value = value,
					Time = dateTime.ToLongDateString() + " " + dateTime.ToLongTimeString()
				});
			}
			else
			{
				return View("Search", new CommitmentData());
			}
		}

		[HttpPost]
		public ActionResult Search()
		{
			DateTime dateTime;
			decimal value;
			var ticker = Request["ticker"];

			if (GetLastValue(ticker, out value, out dateTime))
			{
				return View(new CommitmentData()
				{
					Ticker = ticker,
					Value = value,
					Time = dateTime.ToLongDateString() + " " + dateTime.ToLongTimeString()
				});
			}
			else
			{
				return View(new CommitmentData());
			}
		}

		public ActionResult Display(string id)
		{
			var file = Path.Combine("db", $"{id}.data.json");
			var datetime = DateTime.FromFileTime(long.Parse(id));

			var commitmentDataMap = (FSharpMap<string, ContractExamples.Merkle.AuditPath>)ContractExamples.Oracle.proofMapSerializer.ReadObject(System.IO.File.OpenRead(file));
			var values = ContractExamples.Oracle.priceTable(commitmentDataMap);
			var model = values.Select(t => new Ticker()
			{
				Name = t.Item1,
				Value = t.Item2
			});

			ViewData["Time"] = datetime.ToLongDateString() + " " + datetime.ToLongTimeString();

			return View(model);
		}

		public ActionResult GetData(string ticker)
		{
            string data;

			GetLastData(ticker, out data);

			return Content(data, "application/json");
		}

		bool GetLastValue(string ticker, out decimal value, out DateTime dateTime)
		{
			value = 0;
			dateTime = DateTime.Now;

			try
			{
				var commitments = Directory.GetFiles("db", "*.data.json")
					.Select(t => Regex.Replace(t, @"[^\d]", ""))
					.OrderByDescending(t => t);

				if (commitments.Count() == 0)
				{
					return false;
				}

				foreach (var item in commitments)
				{
					dateTime = DateTime.FromFileTime(long.Parse(item));

					var file = Path.Combine("db", $"{item}.data.json");

					var commitmentDataMap = (FSharpMap<string, ContractExamples.Merkle.AuditPath>)ContractExamples.Oracle.proofMapSerializer.ReadObject(System.IO.File.OpenRead(file));
					var values = ContractExamples.Oracle.priceTable(commitmentDataMap);

					foreach (var _value in values)
					{
						if (_value.Item1 == ticker)
						{
							value = _value.Item2;
							return true;
						}
					}
				}
			}
			catch
			{
			}

			return false;
		}

	    bool GetLastData(string ticker, out string data)
		{
			data = null;

			try
			{
				var commitments = Directory.GetFiles("db", "*.data.json")
					.Select(t => Regex.Replace(t, @"[^\d]", ""))
					.OrderByDescending(t => t);

				if (commitments.Count() == 0)
				{
					return false;
				}

				foreach (var item in commitments)
				{
					//dateTime = DateTime.FromFileTime(long.Parse(item));

					var file = Path.Combine("db", $"{item}");
					var mapFile = Path.ChangeExtension(file, ".data.json");

					var commitmentDataMap = (FSharpMap<string, ContractExamples.Merkle.AuditPath>)ContractExamples.Oracle.proofMapSerializer.ReadObject(System.IO.File.OpenRead(mapFile));

					foreach (var _value in commitmentDataMap)
					{
						if (_value.Key == ticker)
						{
							var outpointFile = Path.ChangeExtension(file, ".outpoint.txt");
                            var outpointData = Convert.FromBase64String(System.IO.File.ReadAllText(outpointFile));
                            var outpoint = Consensus.Serialization.context.GetSerializer<Types.Outpoint>().UnpackSingleObject(outpointData);

                            data = ContractExamples.Oracle.rawData.Invoke(new Tuple<ContractExamples.Merkle.AuditPath, Types.Outpoint>(_value.Value, outpoint));

							return true;
						}
					}
				}
			}
			catch
			{
			}

			return false;
		}
	}
}