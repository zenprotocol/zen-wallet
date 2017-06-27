using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using BlockChain.Data;
using ContractsDiscovery.Web.App_Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
			object model = null;

			switch (id)
			{
				case "CallOption":
					var createCallOption = new CreateCallOption();
                    var oracleStatus = GetOracleStatus().Result;

                    if (oracleStatus == null)
                    {
						createCallOption.OracleErrorMessage = "Oracle not operatable";
					}
                    else if ((bool)oracleStatus["isSetup"] == true)
                    {
                        createCallOption.Oracle.SetValue(oracleStatus["address"].ToString());
                    }
                    else
                    {
                        createCallOption.OracleErrorMessage = oracleStatus["message"].ToString();
                    }

					model = createCallOption;
					break;
				case "TokenGenerator":
					model = new CreateTokenGenerator();
					break;
				case "Oracle":
					model = new CreateOracle();
					break;
			}

			return View(id, model);
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
            createCallOption.Strike.SetValue(Request["strike"]);
			createCallOption.MinimumCollateralRatio.SetValue(Request["minimumCollateralRatio"]);
			createCallOption.OwnerPubKey.SetValue(Request["ownerPubKey"]);

			if (createCallOption.Invalid)
            {
                return View("CallOption", createCallOption);
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
                        createCallOption.Strike.Decimal,
						createCallOption.MinimumCollateralRatio.Decimal,
                        createCallOption.OwnerPubKey.PublicKey);

                    var contract = ContractExamples.QuotedContracts.callOptionFactory(callOptionParameters);
                    var contractCode = ContractExamples.Execution.quotedToString(contract);
					
                    var code = Utils.Dos2Unix(contractCode);
                    var hash = Convert.ToBase64String(Consensus.Merkle.innerHash(Encoding.ASCII.GetBytes(contractCode)));

					ViewBag.Code = code;
					ViewBag.Hash = hash;
                } catch (Exception e) {
                    ViewBag.Message = "Error creating contract: " + e.Message;
                }

				return View("Result");
			}
		}

		public ActionResult Oracle()
		{
			var createOracle = new CreateOracle();

			createOracle.OwnerPubKey.SetValue(Request["ownerPubKey"]);

			if (createOracle.Invalid)
			{
				return View("Oracle", createOracle);
			}
			else
			{
				try
				{
					var oracleParameters = new ContractExamples.QuotedContracts.OracleParameters(
						createOracle.OwnerPubKey.Address.Bytes);

                    var contract = ContractExamples.QuotedContracts.oracleFactory(oracleParameters);
					var contractCode = ContractExamples.Execution.quotedToString(contract);

					var code = Utils.Dos2Unix(contractCode);
					var hash = Convert.ToBase64String(Consensus.Merkle.innerHash(Encoding.ASCII.GetBytes(contractCode)));

					ViewBag.Code = code;
					ViewBag.Hash = hash;
				}
				catch (Exception e)
				{
					ViewBag.Message = "Error creating contract: " + e.Message;
				}

				return View("Result");
			}
		}

		public ActionResult TokenGenerator()
		{
			var createTokenGenerator = new CreateTokenGenerator();

			createTokenGenerator.Destination.SetValue(Request["destination"]);

			if (createTokenGenerator.Invalid)
			{
				return View("TokenGenerator", createTokenGenerator);
			}
			else
			{
				try
				{
					var secureTokenParameters = new ContractExamples.QuotedContracts.SecureTokenParameters(
											createTokenGenerator.Destination.Address.Bytes);

					var contract = ContractExamples.QuotedContracts.secureTokenFactory(secureTokenParameters);
					var contractCode = ContractExamples.Execution.quotedToString(contract);

					var code = Utils.Dos2Unix(contractCode);
					var hash = Convert.ToBase64String(Consensus.Merkle.innerHash(Encoding.ASCII.GetBytes(contractCode)));

					ViewBag.Code = code;
                    ViewBag.Hash = hash;
				}
				catch (Exception e)
				{
					ViewBag.Message = "Error creating contract: " + e.Message;
				}

				return View("Result");
			}
		}

		async Task<JObject> GetOracleStatus()
		{
			string oracleService = WebConfigurationManager.AppSettings["oracleService"];
			var uri = new Uri($"{oracleService}/Data/Status");

			try
			{
				var response = await new HttpClient().GetAsync(uri.AbsoluteUri).ConfigureAwait(false);

				if (response.IsSuccessStatusCode)
				{
                    return JObject.Parse(await response.Content.ReadAsStringAsync());
				}
				else
				{
					return null;
				}
			}
			catch
			{
				return null;
			}
		}
    }
}