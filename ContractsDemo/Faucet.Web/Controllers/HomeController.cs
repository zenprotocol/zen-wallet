using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using Wallet.core.Data;
using Zen.RPC;
using Zen.RPC.Common;

namespace Faucet.Web.Controllers
{
    public class HomeController : Controller
    {
        string address = WebConfigurationManager.AppSettings["node"];

        public async Task<ActionResult> Index()
        {
            var result = await Client.Send<HelloResultPayload>(address, new HelloPayload());

            ViewData["Success"] = result.Success;

            return View();
        }

		[HttpPost]
		public async Task<ActionResult> GetTokens()
        {
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
                Amount = 10
			});

            ViewData["message"] = result.Message;
            ViewData["success"] = result.Success;
			return View(); 
        }
    }
}
