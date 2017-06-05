using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using ContractsDiscovery.Web.App_Data;
using Newtonsoft.Json;
using Zen.RPC;
using Zen.RPC.Common;

namespace ContractsDiscovery.Web.Controllers
{
    public class ActiveContractController : Controller
    {
        string address = WebConfigurationManager.AppSettings["node"];

        public async Task<ActionResult> Index(string id)
        {
			var contractData = new App_Data.ContractData() { ActiveContract = new ActiveContract() };

			Directory.CreateDirectory(Path.Combine("db", "contracts"));
			Directory.CreateDirectory(Path.Combine("db", "asset-metadata"));

            var contractHash = HttpServerUtility.UrlTokenDecode(id);
			var file = $"{id}.json";

            string contractCode = null;
            var codeFile = Path.Combine("db", "contracts", file);
			if (System.IO.File.Exists(codeFile))
			{
				contractCode = System.IO.File.ReadAllText(codeFile);
				contractData.ActiveContract.Code = contractCode;
			}

            var assetMetaDataFile = Path.Combine("db", "asset-metadata", file);
            if (System.IO.File.Exists(assetMetaDataFile))
            {
                try
                {
                    var json = System.IO.File.ReadAllText(assetMetaDataFile);
                    var assetMetaData = JsonConvert.DeserializeObject<AssetMetadata>(json);

					contractData.ActiveContract.AssetName = assetMetaData.name;
					contractData.ActiveContract.AssetImageUrl = assetMetaData.imageUrl;
					contractData.ActiveContract.AssetMetadataVersion = assetMetaData.version;

                } catch
                {
					contractData.ActiveContract.AssetName = "Bas metadata";
                }
            }

			//var contractCodeResult = await Client.Send<GetContractCodeResultPayload>(address, new GetContractCodePayload() { 
			//    Hash = new Wallet.core.Data.Address(contractHash).Bytes 
			//});

			//if (!contractCodeResult.Success)
			//     return View(new ActiveContract() { AuthorMessage = "Error parsing contract code: " + e.Message });

			//var contractTotalAssetsResult = await Client.Send<GetContractTotalAssetsResultPayload>(address, new GetContractTotalAssetsPayload()
			//{
			//	Hash = new Wallet.core.Data.Address(Convert.ToBase64String(contractHash)).Bytes
			//});


			contractData.ActiveContract.Address = Convert.ToBase64String(contractHash);

			if (contractCode != null)
            {
                try
                {
                    var header = contractCode.Split(Environment.NewLine.ToCharArray())[0].Substring(2).Trim();

                    dynamic headerJson = JsonConvert.DeserializeObject(header);

					contractData.ActiveContract.AuthorMessage = headerJson.message;
					contractData.ActiveContract.Type = headerJson.type;
					contractData.ActiveContract.Expiry = headerJson.expiry;
					contractData.ActiveContract.Strike = headerJson.strike;
					contractData.ActiveContract.Underlying = headerJson.underlying;
					contractData.ActiveContract.Oracle = headerJson.oracle;
					contractData.ActiveContract.Code = contractCode;
					// TotalAssets = contractTotalAssetsResult.Confirmed + contractTotalAssetsResult.Unconfirmed,

                } catch (Exception e)
                {
					contractData.ActiveContract.Type = "Bad header";
				}
			}

			return View(contractData);
        }
    }
}
