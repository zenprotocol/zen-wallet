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
using System.Web.UI.WebControls;
using Consensus;
using ContractsDiscovery.Web.App_Data;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Wallet.core.Data;
using Zen.RPC;
using Zen.RPC.Common;
using static ContractExamples.Execution;

namespace ContractsDiscovery.Web.Controllers
{
    public class ContractInteractionController : Controller
    {
        string _address = WebConfigurationManager.AppSettings["node"];
		const byte OPCODE_BUY = 0x01;
		const byte OPCODE_COLLATERALIZE = 0x00;
		const byte OPCODE_EXERCISE = 0x02;
		const byte OPCODE_CLOSE = 0x03;

		[HttpPost]
		public ActionResult PrepareAction()
		{
			var action = Request["Action"];
			var address = Request["ActiveContract.Address"];

			var contractInteraction = new ContractInteraction()
            {
                Action = action,
                Address = address
            };

			return View(contractInteraction);
		}

		public UInt64 ByteArrayToUInt64(byte[] data)
		{
			if (BitConverter.IsLittleEndian)
				Array.Reverse(data);

			return System.BitConverter.ToUInt64(data, 0);
		}

		public UInt64 ParseCollateralData(byte[] data)
		{
			try
			{
				return ByteArrayToUInt64(data.Skip(8).Take(15).ToArray());
			}
			catch
			{
				return 0;
			}
		}

        [HttpPost]
        public async Task<ActionResult> Action()
        {
            var action = Request["Action"];
            var args = new Dictionary<string, string>();
			byte opcode = default(byte);

			var address = Request["Address"];

			var contractHash = new Address(address).Bytes;

			string key = HttpServerUtility.UrlTokenEncode(contractHash);
			var file = $"{key}";

			string contractCode = null;
			var codeFile = Path.ChangeExtension(Path.Combine("db", "contracts", file), ".txt");
            if (System.IO.File.Exists(codeFile))
            {
                contractCode = System.IO.File.ReadAllText(codeFile);
            }

            var contractInteraction = new ContractInteraction()
			{
				Action = action,
				Address = new Address(contractHash, AddressType.Contract).ToString()
			};

            ContractMetadata contractMetadata = null;

			try
			{
				var _metadata = ContractExamples.Execution.metadata(contractCode);

				if (FSharpOption<ContractMetadata>.get_IsNone(_metadata))
				{
					contractInteraction.Message = "No metadata";
				}
                else
                {
                    contractMetadata = _metadata.Value;
                }
			}
            catch
			{
				contractInteraction.Message = "Error getting metadata";
				return View(contractInteraction);
			}

            if (contractMetadata.IsCallOption)
            {
                var callOptionParameters = 
                    (ContractExamples.Execution.ContractMetadata.CallOption)contractMetadata;
                switch (action)
                {
                    case "Collateralize":
						var pkAddress = new PKAddressField();
						pkAddress.SetValue(Request["return-address"]);

						if (pkAddress.Invalid)
						{
			                contractInteraction.Message = "Invalid return address";
							return View(contractInteraction);
						}

						args.Add("returnPubKeyAddress", pkAddress.Value);
                        opcode = OPCODE_COLLATERALIZE;
                        break;
                    case "Exercise":
						var pkExerciseReturnAddress = new PKAddressField();
						pkExerciseReturnAddress.SetValue(Request["exercise-return-address"]);

						if (pkExerciseReturnAddress.Invalid)
						{
			                contractInteraction.Message = "Invalid send address";
							return View(contractInteraction);
						}

						args.Add("returnPubKeyAddress", pkExerciseReturnAddress.Value);
						
                        var oracleData = GetOracleCommitmentData(callOptionParameters.Item.underlying, DateTime.Now.ToUniversalTime()).Result;
                        args.Add("oracleRawData", oracleData);
                        opcode = OPCODE_EXERCISE;
                        break;
					case "Buy":
						var pkSendAddress = new PKAddressField();
						pkSendAddress.SetValue(Request["buy-send-address"]);

						if (pkSendAddress.Invalid)
						{
			                contractInteraction.Message = "Invalid send address";
							return View(contractInteraction);
						}

						args.Add("returnPubKeyAddress", pkSendAddress.Value);
						opcode = OPCODE_BUY;
						break;
					case "Close":
						opcode = OPCODE_CLOSE;
						break;	
                }
            }

            var argsMap = new FSharpMap<string, string>(args.Select(t => new Tuple<string, string>(t.Key, t.Value)));
            var result = await Client.Send<GetContractPointedOutputsResultPayload>(_address, new GetContractPointedOutputsPayload() { ContractHash = contractHash });
            var utxos = GetContractPointedOutputsResultPayload.Unpack(result.PointedOutputs);
            var data = ContractUtilities.DataGenerator.makeData(contractMetadata, utxos, opcode, argsMap);

            if (FSharpOption<string>.get_IsNone(data))
            {
                contractInteraction.Message = "No data";
            }
            else
            {
                contractInteraction.Data = data.Value;
            }
          
			return View(contractInteraction);
		}

        async Task<string> GetOracleCommitmentData(string underlying, DateTime time)
        {
			string oracleService = WebConfigurationManager.AppSettings["oracleService"];
			var uri = new Uri($"{oracleService}/Data/GetData?ticker={underlying}");

            try
            {
                var response = await new HttpClient().GetAsync(uri.AbsoluteUri).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
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
