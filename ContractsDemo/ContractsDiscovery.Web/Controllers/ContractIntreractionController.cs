using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using Consensus;
using ContractsDiscovery.Web.App_Data;
using Newtonsoft.Json;
using Wallet.core.Data;
using Zen.RPC;
using Zen.RPC.Common;

namespace ContractsDiscovery.Web.Controllers
{
    public class ContractInteractionController : Controller
    {
        string address = WebConfigurationManager.AppSettings["node"];
		const byte OPCODE_BUY = 0x01;

		[HttpPost]
		public ActionResult PrepareAction()
		{
			var action = Request["action"];
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
			var address = Request["Address"];

			var contractInteraction = new ContractInteraction()
			{
				Action = action,
				Address = address
			};

            switch (action)
            {
                case "Buy":
					var buyAmount = Request["buy-amount"];
					var buySendAddress = Request["buy-send-address"];
					var contractHash = new Wallet.core.Data.Address(address).Bytes;

					Address buyerAddress = null;

					try
					{

						buyerAddress = new Wallet.core.Data.Address(buySendAddress);
					}
					catch
					{
						contractInteraction.Message = "Invalid address";
					}

					try 
					{
						var contractCodeResult = await Client.Send<GetContractCodeResultPayload>(address, new GetContractCodePayload()
						{
							Hash = contractHash
						});

						var code = Encoding.ASCII.GetString(contractCodeResult.Code);
						var header = code.Split(Environment.NewLine.ToCharArray())[0].Substring(2).Trim();

						dynamic headerJson = JsonConvert.DeserializeObject(header);
						var controlAsset = Convert.FromBase64String(headerJson.controlAsset.ToString());
						var numeraire = Convert.FromBase64String(headerJson.numeraire.ToString());

						var getDataOutpput = await Client.Send<GetPointedOutpointResultPayload>(address, new GetPointedOutpointPayload()
						{
							IsContract = true,
							Asset = controlAsset,
							Address = contractHash
						});

						var data = ((Types.OutputLock.ContractLock)getDataOutpput.Output.@lock).data;

						var collateral = ParseCollateralData(data);

						var getFundsOutpput = await Client.Send<GetPointedOutpointResultPayload>(address, new GetPointedOutpointPayload()
						{
							IsContract = true,
							Asset = numeraire,
							Address = contractHash
						});

						var funds = getFundsOutpput.Output.spend.amount;

						if (collateral != funds)
							throw new Exception("mismatch");

						var bytes1 = new byte[] { OPCODE_BUY }
							.Concat(buyerAddress.Bytes);
						var bytes2 = new byte[] { OPCODE_BUY }
							.Concat(new byte[] { (byte)getDataOutpput.Outpoint.index })
							.Concat(getDataOutpput.Outpoint.txHash)
							.Concat(new byte[] { (byte)getFundsOutpput.Outpoint.index })
							.Concat(getFundsOutpput.Outpoint.txHash);

						contractInteraction.Data = System.Convert.ToBase64String(bytes1.ToArray()) + " " + System.Convert.ToBase64String(bytes2.ToArray());
						contractInteraction.Amount = buyAmount;
					}
					catch (Exception e)
					{
						contractInteraction.Message = "Error";
					}
					break;
			}

			return View(contractInteraction);
		}
	}
}
