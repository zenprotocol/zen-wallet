using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
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

                        string oracleData; //GetOracleCommitmentData(callOptionParameters.Item.underlying, DateTime.Now.ToUniversalTime()).Result;

                        if (GetLastData(callOptionParameters.Item.underlying, out oracleData))
                            args.Add("oracleRawData", oracleData);
                        else
						{
							contractInteraction.Message = "Error getting oracle data";
							return View(contractInteraction);
						}                           

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
            var data = ContractUtilities.DataGenerator.makeJson(contractMetadata, utxos, opcode, argsMap);

            if (data.IsError)
            {
                contractInteraction.Message = data.ErrorValue.ToString();
            }
            else
            {
                contractInteraction.Data = data.ResultValue.JsonValue.ToString();
            }
          
			return View(contractInteraction);
		}

		bool GetLastData(string ticker, out string data)
		{
			data = null;

			try
			{
				var commitments = Directory.GetFiles("db", "*.data.json")
					.Select(t => Regex.Replace(t, @"[^\d]", ""))
					.OrderByDescending(t => t);

				if (commitments.Count() == 0)
				{
					return false;
				}

				foreach (var item in commitments)
				{
					//dateTime = DateTime.FromFileTime(long.Parse(item));

					var file = Path.Combine("db", $"{item}");
					var mapFile = Path.ChangeExtension(file, ".data.json");

					var commitmentDataMap = (FSharpMap<string, Tuple<byte[],uint,byte[][]>>)ContractExamples.Oracle.proofMapSerializer.ReadObject(System.IO.File.OpenRead(mapFile));

					foreach (var _value in commitmentDataMap)
					{
						if (_value.Key == ticker)
						{
							var outpointFile = Path.ChangeExtension(file, ".outpoint.txt");
							var outpointData = Convert.FromBase64String(System.IO.File.ReadAllText(outpointFile));
							var outpoint = Consensus.Serialization.context.GetSerializer<Types.Outpoint>().UnpackSingleObject(outpointData);

							data = ContractExamples.Oracle.rawData.Invoke(new Tuple<Tuple<byte[], uint, byte[][]>, Types.Outpoint>(_value.Value, outpoint));

							return true;
						}
					}
				}
			}
			catch
			{
			}

			return false;
		}
	}
}
