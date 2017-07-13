using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using Wallet.core.Data;
using Zen.RPC;
using Zen.RPC.Common;

namespace ContractsDiscovery.Web.Controllers
{
    public class FaucetController : Controller
    {
		string address = WebConfigurationManager.AppSettings["node"];

		public async Task<ActionResult> Index()
		{
			var contractManager = new FaucetManager();

			if (!contractManager.IsSetup)
			{
				ViewBag.Message = contractManager.Message;
			}

			return View();
		}

		[HttpPost]
		public async Task<ActionResult> GetTokens()
		{
			var contractManager = new FaucetManager();

			if (!contractManager.IsSetup)
			{
				ViewData["message"] = contractManager.Message;
				ViewData["success"] = false;
				return View();
			}

            Address sendAddress = null;

			try
			{
				sendAddress = new Wallet.core.Data.Address(Request["address"]);
			}
			catch
			{
				ViewData["message"] = "Invalid address";
				ViewData["success"] = false;
				return View();
			}

			var result = await Client.Send<ResultPayload>(address, new MakeTransactionPayload()
			{
				Asset = Consensus.Tests.zhash,
				Address = sendAddress.ToString(),
				Amount = 50000000
			});

			ViewData["message"] = result.Message;
			ViewData["success"] = result.Success;
			return View();
		}
    }
}
