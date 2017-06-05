using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Datafeed.Web.Models;
using Newtonsoft.Json;

namespace Datafeed.Web.Controllers
{
    public class CommitmentController : Controller
    {
        public ActionResult Index(string commitmentId)
        {
			var data = System.IO.File.ReadAllText($"{commitmentId}.json");
            var datetime = DateTime.FromFileTime(long.Parse(commitmentId));

			var json = JsonConvert.DeserializeObject<Commitment>(data);

			ViewData["MerkelRoot"] = Convert.ToBase64String(json.merkelRoot);
            ViewData["Time"] = datetime.ToLongDateString() + " " + datetime.ToLongTimeString();

			return View(json.items);
        }
    }
}
