using System;
using System.IO;
using System.Text;
using ContractsDiscovery.Web.App_Data;
using Microsoft.FSharp.Core;

namespace ContractsDiscovery.Web
{
    public class Utils
    {
		//     public static ActiveContract GetActiveContract(ContractData contractData)
		//     {
		//       //  var code = contractData.
		//dynamic headerJson = JsonConvert.DeserializeObject(header);

		//var c = new ActiveContract()
		//{
		//	AuthorMessage = headerJson.message,
		//	Hash = contractHash,
		//	Type = headerJson.type,
		//	Expiry = headerJson.expiry,
		//	Strike = headerJson.strike,
		//	Underlying = headerJson.underlying,
		//	Oracle = headerJson.oracle,
		//	Code = contractCodeResult.Code
		//};

		//return View(new ContractData() { ActiveContract = c });
		//}

		public static string Dos2Unix(string value)
		{
			const byte CR = 0x0D;
			const byte LF = 0x0A;

            byte[] data = Encoding.UTF8.GetBytes(value);
            var ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			int position = 0;
			int index = 0;

			do
			{
				index = Array.IndexOf<byte>(data, CR, position);
				if ((index >= 0) && (data[index + 1] == LF))
				{
					// Write before the CR
					bw.Write(data, position, index - position);
					// from LF
					position = index + 1;
				}
			}
			while (index >= 0);
			bw.Write(data, position, data.Length - position);

			ms.Position = 0;
			var sr = new StreamReader(ms);
			return sr.ReadToEnd();
		}

		public static void SetContractInfo(ActiveContract activeContract, string contractCode)
		{
			try
			{
				var metadata = ContractExamples.Execution.metadata(contractCode);

				if (FSharpOption<ContractExamples.Execution.ContractMetadata>.get_IsSome(metadata))
				{
					if (metadata.Value.IsCallOption)
					{
                        var callOptionParameters = ((ContractExamples.Execution.ContractMetadata.CallOption)metadata.Value).Item;

                        activeContract.Underlying = callOptionParameters.underlying;

						activeContract.Type = "call-option";
						activeContract.Description = "Call Option";
						activeContract.Premium = String.Format("{0:0.00#####}", callOptionParameters.price);
						activeContract.Strike = String.Format("{0:0.00#####}", callOptionParameters.strike);
						activeContract.Oracle = new Wallet.core.Data.Address(callOptionParameters.oracle, Wallet.core.Data.AddressType.Contract).ToString();

					}
					else if (metadata.Value.IsOracle)
					{
						activeContract.Type = "oracle";
						activeContract.Description = "Oracle";
					}
					else if (metadata.Value.IsSecureToken)
					{
						activeContract.Type = "secure-token-generator";
						activeContract.Description = "Secure Token Generator";
					}
					else
					{
						activeContract.Type = "";
						activeContract.Description = "Unrecognized";
					}
				}
				else
				{
					activeContract.Type = "Unknown";
				}
			}
			catch (Exception e)
			{
				activeContract.Type = "Error getting metadata";
			}
		}

		public static string GetTemplate(string name)
		{
			var fileName = Path.ChangeExtension(Path.Combine("Templates", name), ".txt");

			return File.ReadAllText(fileName);
		}
    }
}
