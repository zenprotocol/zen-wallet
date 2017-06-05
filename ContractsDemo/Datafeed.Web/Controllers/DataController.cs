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

namespace Datafeed.Web.Controllers
{
    public class DataController : Controller
    {
		public ActionResult Index()
		{
			return View();
		}

        [HttpPost]
        public ActionResult Search() {
            var ticker = Request["ticker"];

            var commitmentData = new Datafeed.Web.App_Data.CommitmentData();

            foreach (var commitmentId in Directory.GetFiles(".", "*.json")
                                  .Select(t => Regex.Replace(t, @"[^\d]", ""))
                 .OrderByDescending(t => t))
            {
				var data = System.IO.File.ReadAllText($"{commitmentId}.json");
                var datetime = DateTime.FromFileTime(long.Parse(commitmentId));
				var json = JsonConvert.DeserializeObject<Commitment>(data);

                foreach (var item in json.items)
                {
                    if (item.Name == ticker)
                    {
                        commitmentData.Id = commitmentId;
                        commitmentData.Ticker = ticker;
                        commitmentData.Value = item.Value;
                        commitmentData.Data = Convert.ToBase64String(json.merkelRoot);
                        commitmentData.Time = datetime.ToLongDateString() + " " + datetime.ToLongTimeString();
                    }
                }
            }

            return View(commitmentData);
        }
    }
}
