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
using ContractsDiscovery.Web.App_Data;
using Newtonsoft.Json;
using Zen.RPC;
using Zen.RPC.Common;

namespace ContractsDiscovery.Web.Controllers
{
    public class ContractCreationController : Controller
    {
        public ActionResult Index()
        {
			return View();
        }

		public ActionResult FromTemplate(string id)
		{
			if (id == "CallOption")
				return View(new CreateCallOption());
			
			return View();
		}

		public ActionResult CallOption()
		{
			var createCallOption = new CreateCallOption();

            createCallOption.Numeraire.SetValue(Request["numeraire"]);
            createCallOption.ControlAsset.SetValue(Request["controlAsset"]);
			createCallOption.Oracle.SetValue(Request["oracle"]);
			createCallOption.ControlAssetReturn.SetValue(Request["controlAssetReturn"]);
			createCallOption.Underlying.SetValue(Request["underlying"]);
			createCallOption.Price.SetValue(Request["price"]);
			createCallOption.MinimumCollateralRatio.SetValue(Request["minimumCollateralRatio"]);
			createCallOption.OwnerPubKey.SetValue(Request["ownerPubKey"]);

			if (createCallOption.Invalid)
            {
                return View("FromTemplate", createCallOption);
            }
            else
            {
                try {
                    var callOptionParameters = new ContractExamples.QuotedContracts.CallOptionParameters(
                        createCallOption.Numeraire.Address.Bytes,
                        createCallOption.ControlAsset.Address.Bytes,
                        createCallOption.ControlAssetReturn.Address.Bytes,
                        createCallOption.Oracle.Address.Bytes,
                        createCallOption.Underlying.Value,
                        createCallOption.Price.Decimal,
                        createCallOption.MinimumCollateralRatio.Decimal,
                        createCallOption.OwnerPubKey.Address.Bytes);

                    var contract = ContractExamples.QuotedContracts.callOptionFactory(callOptionParameters);
                    var contractCode = ContractExamples.Execution.quotedToString(contract);

					var json = JsonConvert.SerializeObject(new
					{
						message = "This is a generated call-option",
						publicKey = "xxx",
						type = "call-option",
						strike = "",
						oracle = createCallOption.Oracle.Address.ToString(),
						underlying = createCallOption.Underlying.Value,
						controlAsset = createCallOption.ControlAsset.Address.ToString(),
						numeraire = createCallOption.Numeraire.Address.ToString()
					});

                    var code = Utils.Dos2Unix("// " + json + "\n\n" + contractCode);

                    ViewBag.Code = code;
                } catch (Exception e) {
                    ViewBag.Message = "Error creating contract: " + e.Message;
                }

				return View("Result");
			}
		}
    }
}