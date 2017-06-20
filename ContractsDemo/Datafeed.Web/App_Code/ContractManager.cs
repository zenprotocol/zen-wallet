using System;
using System.Web.Configuration;
using Zen.RPC.Common;
using Zen.RPC;
using System.Linq;
using Consensus;
using Sodium;
using System.Text;
using System.Collections.Generic;

namespace Datafeed.Web.App_Code
{
	public class Utils
	{
		public static string NodeRPCAddress = WebConfigurationManager.AppSettings["node"];
		static string walletPrivateKey = WebConfigurationManager.AppSettings["walletPrivateKey"];

        public bool EnsureContract(out string errorMessage)
        {
            if (!EnsureFunds())
            {
                errorMessage = "No funds";
                return false;
            }

            var keypair = EnsureKeyPair();
            var contractCode = EnsureContractCode(keypair.PublicKey);

            if (!EnsureContractActive(contractCode))
			{
				errorMessage = "Could not activate Contract";
				return false;
			}

            if (!EnsureInitialOutpoiunt(contractCode))
			{
				errorMessage = "Could not create initial outpoint";
				return false;
			}

            errorMessage = "";
			return true;
        }

		static bool EnsureFunds()
		{
			if (HasFunds())
			{
				return true;
			}

			if (!Acquire())
			{
				return false;
			}

			return HasFunds();
		}

		static bool HasFunds()
		{
			var getBalanceResultPayload = Client.Send<GetBalanceResultPayload>(NodeRPCAddress, new GetBalancePayload() { Asset = Consensus.Tests.zhash }).Result;

			if (!getBalanceResultPayload.Success)
			{
				return false;
			}

			return getBalanceResultPayload.Balance > 0;
		}

	    static bool Acquire()
		{
			return Client.Send<ResultPayload>(NodeRPCAddress, new AcquirePayload() { PrivateKey = walletPrivateKey }).Result.Success;
		}

        static KeyPair EnsureKeyPair()
        {
			if (!System.IO.File.Exists("oracle-key.txt"))
			{
				var keyPair = PublicKeyAuth.GenerateKeyPair();
				System.IO.File.WriteAllText("oracle-key.txt", Convert.ToBase64String(keyPair.PrivateKey) + " " + Convert.ToBase64String(keyPair.PublicKey));
                return keyPair;
            }
			else
			{
				var data = System.IO.File.ReadAllText("oracle-key.txt");
				var parts = data.Split(null);
                return new KeyPair(Convert.FromBase64String(parts[0]), Convert.FromBase64String(parts[1]));
			}
        }

        static string EnsureContractCode(byte[] publicKey)
        {
            if (System.IO.File.Exists("oracle-contract.txt"))
            {
                return System.IO.File.ReadAllText("oracle-contract.txt");
            }
            else
            {
                var @params = new ContractExamples.QuotedContracts.OracleParameters(publicKey);
                var contract = ContractExamples.QuotedContracts.oracleFactory(@params);
                var contractCode = ContractExamples.Execution.quotedToString(contract);
                System.IO.File.WriteAllText("oracle-contract.txt", contractCode);
                return contractCode;
			}
        }

        static bool EnsureContractActive(string contractCode)
        {
            if (IsContractActive(contractCode))
			{
				return true;
			}

            return ActivateContract(contractCode);
        }

        static bool IsContractActive(string contractCode)
        {
			var acsResult = Client.Send<GetACSResultPayload>(NodeRPCAddress, new GetACSPayload()).Result;

			if (!acsResult.Success)
			{
				return false;
			}

			var contractHash = Merkle.innerHash(Encoding.ASCII.GetBytes(contractCode));

            return acsResult.Contracts.Count(t => t.Hash.SequenceEqual(contractHash)) == 1;
		}

        static bool ActivateContract(string contractCode)
        {
			var activateContractResult = Client.Send<ResultPayload>(NodeRPCAddress, new ActivateContractPayload() { Code = contractCode, Blocks = 1 }).Result;

            return activateContractResult.Success;
		}

		static bool EnsureInitialOutpoiunt(string contractCode)
		{
			if (HasInitialOutpoint(contractCode))
			{
				return true;
			}

            return SetInitialOutpoint(contractCode);
		}

		static bool HasInitialOutpoint(string contractCode)
		{
			var contractHash = Merkle.innerHash(Encoding.ASCII.GetBytes(contractCode));

			var getOutpointsResult =
				Client.Send<GetContractPointedOutputsResultPayload>(NodeRPCAddress, new GetContractPointedOutputsPayload() { ContractHash = contractHash }).Result;

			if (!getOutpointsResult.Success)
			{
				return false;
			}

            return getOutpointsResult.PointedOutputs.Count > 0;
		}

		static bool SetInitialOutpoint(string contractCode)
        {
			var contractHash = Merkle.innerHash(Encoding.ASCII.GetBytes(contractCode));
			var contractAddress = new Wallet.core.Data.Address(contractHash, Wallet.core.Data.AddressType.Contract);

			var sendContractResult = Client.Send<ResultPayload>(NodeRPCAddress, new SpendPayload() { Address = contractAddress.ToString(), Amount = 1 }).Result;

            return sendContractResult.Success;
		}
	}
}