using System;
using System.Web.Configuration;
using Zen.RPC.Common;
using Zen.RPC;
using System.Linq;
using Consensus;
using System.Text;
using System.Collections.Generic;
using Wallet.core.Data;

namespace ContractsDiscovery.Web
{
	public class FaucetManager
	{
		static string NodeRPCAddress = WebConfigurationManager.AppSettings["node"];
		static string faucetPrivateKey = WebConfigurationManager.AppSettings["faucetPrivateKey"];

        public string Message { get; private set; }
		public bool IsSetup { get; private set; }

		public FaucetManager()
        {
            if (!EnsureKeyAcquired())
            {
                Message = "Could not import wallet key for faucet.";
                IsSetup = false;
                return;
            }

            Message = "";
			IsSetup = true;
        }

		//bool EnsureFunds()
		//{
		//	if (HasFunds())
		//	{
		//		return true;
		//	}

		//	if (!Acquire())
		//	{
		//		return false;
		//	}

		//	return HasFunds();
		//}

		//bool HasFunds()
		//{
		//	var getBalanceResultPayload = Client.Send<GetBalanceResultPayload>(NodeRPCAddress, new GetBalancePayload() { Asset = Consensus.Tests.zhash }).Result;

		//	if (!getBalanceResultPayload.Success)
		//	{
		//		return false;
		//	}

		//	return getBalanceResultPayload.Balance > 0;
		//}

		bool EnsureKeyAcquired()
		{
			return Client.Send<ResultPayload>(NodeRPCAddress, new EnsureTestKeyAcquiredPayload() { PrivateKey = faucetPrivateKey }).Result.Success;
		}
	}
}