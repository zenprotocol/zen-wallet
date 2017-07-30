using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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
					//               var oracleStatus = GetOracleStatus().Result;

					//               if (oracleStatus == null)
					//               {
					//	createCallOption.OracleErrorMessage = "Oracle not operatable";
					//}

					var oracleContractManager = new ContractsDiscovery.Web.App_Code.OracleContractManager();

                    if (oracleContractManager.IsSetup)
                    {
                        createCallOption.Oracle.SetValue(oracleContractManager.ContractAddress.ToString());
                    }
                    else
                    {
                        createCallOption.OracleErrorMessage = oracleContractManager.Message;
                    }

                    createCallOption.ControlAssets = GetSecureTokens();
					//createCallOption.OracleServiceUrl = WebConfigurationManager.AppSettings["oracleService"];
                    createCallOption.Tickers.AddRange(new List<String>(WebConfigurationManager.AppSettings["tickers"].Split(',')));

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

					//var contract = ContractExamples.QuotedContracts.callOptionFactory(callOptionParameters);
					//var contractCode = ContractExamples.Execution.quotedToString(contract);
					var tpl = GetTemplate("CallOption");

					var metadata = new
					{
						contractType = "calloption",
						numeraire = Convert.ToBase64String(callOptionParameters.numeraire),
						controlAsset = Convert.ToBase64String(callOptionParameters.controlAsset),
						controlAssetReturn = Convert.ToBase64String(callOptionParameters.controlAssetReturn),
						oracle = Convert.ToBase64String(callOptionParameters.oracle),
						underlying = callOptionParameters.underlying,
						price = "" + callOptionParameters.price,
						strike = "" + callOptionParameters.strike,
						minimumCollateralRatio = "" + callOptionParameters.minimumCollateralRatio,
						ownerPubKey = Convert.ToBase64String(callOptionParameters.ownerPubKey)
					};
                    var jsonHeader = "//" + JsonConvert.SerializeObject(metadata);
					var contractCode = tpl
						 .Replace("__numeraire__", Convert.ToBase64String(callOptionParameters.numeraire))
						 .Replace("__controlAsset__", Convert.ToBase64String(callOptionParameters.controlAsset))
						 .Replace("__controlAssetReturn__", Convert.ToBase64String(callOptionParameters.controlAssetReturn))
						 .Replace("__oracle__", Convert.ToBase64String(callOptionParameters.oracle))
						 .Replace("__underlying__", callOptionParameters.underlying)
						 .Replace("__price__", "" + callOptionParameters.price)
						 .Replace("__strike__", "" + callOptionParameters.strike)
						 .Replace("__minimumCollateralRatio__", "" + callOptionParameters.minimumCollateralRatio)
						 .Replace("__ownerPubKey__", Convert.ToBase64String(callOptionParameters.ownerPubKey));
					contractCode += "\n" + jsonHeader;

                    var code = Utils.Dos2Unix(contractCode);
                    var contractHash = Consensus.Merkle.innerHash(Encoding.ASCII.GetBytes(contractCode));

					ViewBag.Code = code;
					ViewBag.Hash = Convert.ToBase64String(contractHash);

					var file = $"{HttpServerUtility.UrlTokenEncode(contractHash)}";

					Directory.CreateDirectory(Path.Combine("db", "contracts"));
					Directory.CreateDirectory(Path.Combine("db", "asset-metadata"));

					if (!System.IO.File.Exists(file))
					{
						System.IO.File.WriteAllText(Path.ChangeExtension(Path.Combine("db", "contracts", file), ".txt"), contractCode);
					}

					System.IO.File.WriteAllText(Path.ChangeExtension(Path.Combine("db", "asset-metadata", file), ".json"), JsonConvert.SerializeObject(new AssetMetadata()
					{
						name = Request["assetName"]

					}));

                } catch (Exception e) {
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

                    //var contract = ContractExamples.QuotedContracts.secureTokenFactory(secureTokenParameters);
                    //var contractCode = ContractExamples.Execution.quotedToString(contract);
                    var tpl = GetTemplate("SecureToken");

                    var metadata = new { contractType = "securetoken", destination = Convert.ToBase64String(secureTokenParameters.destination) };
					var jsonHeader = "//" + JsonConvert.SerializeObject(metadata);
					var contractCode = tpl.Replace("__ADDRESS__", Convert.ToBase64String(secureTokenParameters.destination));
					contractCode += "\n" + jsonHeader;

					var code = Utils.Dos2Unix(contractCode);
                    var contractHash = Consensus.Merkle.innerHash(Encoding.ASCII.GetBytes(contractCode));

					ViewBag.Code = code;
                    ViewBag.Hash = Convert.ToBase64String(contractHash);

                    var file = $"{HttpServerUtility.UrlTokenEncode(contractHash)}";

                    Directory.CreateDirectory(Path.Combine("db", "contracts"));
					Directory.CreateDirectory(Path.Combine("db", "asset-metadata"));

					if (!System.IO.File.Exists(file))
					{
						System.IO.File.WriteAllText(Path.ChangeExtension(Path.Combine("db", "contracts", file), ".txt"), contractCode);
					}

					System.IO.File.WriteAllText(Path.ChangeExtension(Path.Combine("db", "asset-metadata", file), ".json"), JsonConvert.SerializeObject(new AssetMetadata()
					{
						name = Request["assetName"]
					}));
				}
				catch (Exception e)
				{
					ViewBag.Message = "Error creating contract: " + e.Message;
				}

				return View("Result");
			}
		}

		//async Task<JObject> GetOracleStatus()
		//{
		//	string oracleService = WebConfigurationManager.AppSettings["oracleService"];
		//	var uri = new Uri($"{oracleService}/Data/Status");

		//	try
		//	{
		//		var response = await new HttpClient().GetAsync(uri.AbsoluteUri).ConfigureAwait(false);

		//		if (response.IsSuccessStatusCode)
		//		{
  //                  return JObject.Parse(await response.Content.ReadAsStringAsync());
		//		}
		//		else
		//		{
		//			return null;
		//		}
		//	}
  //          catch //(Exception e)
		//	{
		//		return null;
		//	}
		//}

        Dictionary<string, string> GetSecureTokens()
        {
			return Directory.GetFiles(Path.Combine("db", "contracts"), "*.txt").Select(t =>
			{
				var hash = HttpServerUtility.UrlTokenDecode(System.IO.Path.GetFileNameWithoutExtension(t));
				var code = System.IO.File.ReadAllText(t);

				var activeContract = new ActiveContract();

				activeContract.AddressUrl = HttpServerUtility.UrlTokenEncode(hash);
				activeContract.Address = new Wallet.core.Data.Address(hash, Wallet.core.Data.AddressType.Contract).ToString();

				Utils.SetContractInfo(activeContract, code);

				var id = System.IO.Path.GetFileNameWithoutExtension(t);
				var assetMetaDataFile = Path.Combine("db", "asset-metadata", $"{id}.json");
				if (System.IO.File.Exists(assetMetaDataFile))
				{
					try
					{
						var json = System.IO.File.ReadAllText(assetMetaDataFile);
						var assetMetaData = JsonConvert.DeserializeObject<AssetMetadata>(json);
						activeContract.AssetName = assetMetaData.name;
					}
					catch
					{

					}
				}

				return activeContract;
            })
                            .Where(t=>t.Type == "secure-token-generator")
                            .ToDictionary(t=>t.Address, t=> string.IsNullOrEmpty(t.AssetName) ? t.Address : $"{t.AssetName} ({t.Address})");
		}

		string GetTemplate(string name)
		{
            var fileName = Path.ChangeExtension(Path.Combine("Templates", name), ".txt");

            return System.IO.File.ReadAllText(fileName);
		}
    }
}