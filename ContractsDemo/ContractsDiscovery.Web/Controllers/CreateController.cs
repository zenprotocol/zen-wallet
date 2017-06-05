using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using BlockChain.Data;
using Consensus;
using ContractsDiscovery.Web.App_Data;
using Newtonsoft.Json;
using Zen.RPC;
using Zen.RPC.Common;

namespace ContractsDiscovery.Web.Controllers
{
    public class CreateController : Controller
    {
        string address = WebConfigurationManager.AppSettings["node"];

        public ActionResult Index()
        {
             return View();
        }

		[HttpPost, ValidateInput(false)]
		public ActionResult Post()
        {
			Directory.CreateDirectory(Path.Combine("db", "contracts"));
			Directory.CreateDirectory(Path.Combine("db", "asset-metadata"));

            var contractCode = Utils.Dos2Unix(Request["contractCode"]);
            var contractHash = Merkle.innerHash(Encoding.ASCII.GetBytes(contractCode));
            var file = $"{HttpServerUtility.UrlTokenEncode(contractHash)}.json";

            if (!System.IO.File.Exists(file))
            {
    			System.IO.File.WriteAllText(Path.Combine("db", "contracts", file), contractCode);			
            }

            System.IO.File.WriteAllText(Path.Combine("db", "asset-metadata", file), JsonConvert.SerializeObject(new AssetMetadata()
			{
				name = Request["assetName"],
				imageUrl = Request["assetImageURL"],
				version = Request["assetMatadataVersion"]
			}));

			return View("Index");
        }
	}
}
