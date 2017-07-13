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
using Microsoft.FSharp.Core;
using Newtonsoft.Json;
using Zen.RPC;
using Zen.RPC.Common;

namespace ContractsDiscovery.Web.Controllers
{
    public class HomeController : Controller
    {
        string address = WebConfigurationManager.AppSettings["node"];

        public async Task<ActionResult> Index()
        {
            Directory.CreateDirectory(Path.Combine("db", "contracts"));

            try
            {
                //var helloResult = await Client.Send<HelloResultPayload>(new HelloPayload());
                //ViewData["serverMessage"] = helloResult.Message;

                var acsResult = await Client.Send<GetACSResultPayload>(address, new GetACSPayload());
				var contractsData = new HashDictionary<Zen.RPC.Common.ContractData>();

				foreach (var acs in acsResult.Contracts)
				{
					string key = HttpServerUtility.UrlTokenEncode(acs.Hash);
					contractsData[acs.Hash] = acs;

                    string file = Path.ChangeExtension(Path.Combine("db", "contracts", $"{key}"), ".txt");

					if (!System.IO.File.Exists(file))
					{
						System.IO.File.WriteAllText(file, Encoding.ASCII.GetString(acs.Code));
					}
				}

                var model = Directory.GetFiles(Path.Combine("db", "contracts"), "*.txt").Select(t => 
				{
					var hash = HttpServerUtility.UrlTokenDecode(System.IO.Path.GetFileNameWithoutExtension(t));
					var code = System.IO.File.ReadAllText(t);
				
					var activeContract = new ActiveContract();

					activeContract.AddressUrl = HttpServerUtility.UrlTokenEncode(hash);
					activeContract.Address = new Wallet.core.Data.Address(hash, Wallet.core.Data.AddressType.Contract).ToString();

					if (contractsData.ContainsKey(hash))
					{
						var contractData = contractsData[hash];
						activeContract.LastBlock = contractData.LastBlock;
					}

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
                });

                return View(model);
			}
            catch (Exception e)
            {
				var model = new List<ActiveContract>();
				ViewData["serverMessage"] = "Caught: " + e.Message;
				return View(model);
			}
        }
    }
}
