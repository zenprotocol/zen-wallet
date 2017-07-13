using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ContractsDiscovery.Web.App_Data;
using Newtonsoft.Json;

namespace ContractsDiscovery.Web.Controllers
{
	public class AssetMetadataController : Controller
	{
		public JsonResult Index(string id)
		{
			AssetMetadata assetMetaData = null;

			var assetMetaDataFile = Path.Combine("db", "asset-metadata", $"{id}.json");
			if (System.IO.File.Exists(assetMetaDataFile))
			{
				try
				{
					var json = System.IO.File.ReadAllText(assetMetaDataFile);
					assetMetaData = JsonConvert.DeserializeObject<AssetMetadata>(json);
				}
				catch
				{
					assetMetaData = new AssetMetadata() { name = "Error" };
				}
			}
			else
			{
				assetMetaData = new AssetMetadata();
			}

			return Json(assetMetaData, JsonRequestBehavior.AllowGet);
		}
	}
}
