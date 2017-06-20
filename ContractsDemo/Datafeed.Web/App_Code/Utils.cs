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
		private static string walletPrivateKey = WebConfigurationManager.AppSettings["walletPrivateKey"];

		public static bool EnsureFunds()
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

		public static bool HasFunds()
		{
			var getBalanceResultPayload = Client.Send<GetBalanceResultPayload>(NodeRPCAddress, new GetBalancePayload() { Asset = Consensus.Tests.zhash }).Result;

			if (!getBalanceResultPayload.Success)
			{
				return false;
			}

			return getBalanceResultPayload.Balance > 0;
		}

		public static bool Acquire()
		{
			return Client.Send<ResultPayload>(NodeRPCAddress, new AcquirePayload() { PrivateKey = walletPrivateKey }).Result.Success;
		}

		public static bool EnsureOracle(out byte[] contractHash, out byte[] privateKey)
		{
			byte[] publicKey;

			contractHash = null;

			if (!System.IO.File.Exists("oracle-key.txt"))
			{
				var keyPair = PublicKeyAuth.GenerateKeyPair();
				privateKey = keyPair.PrivateKey;
				publicKey = keyPair.PublicKey;
				System.IO.File.WriteAllText("oracle-key.txt", Convert.ToBase64String(keyPair.PrivateKey) + " " + Convert.ToBase64String(keyPair.PublicKey));
			}
			else
			{
				var data = System.IO.File.ReadAllText("oracle-key.txt");
				var parts = data.Split(null);
				privateKey = Convert.FromBase64String(parts[0]);
				publicKey = Convert.FromBase64String(parts[1]);
			}

			if (System.IO.File.Exists("oracle-contract.txt"))
			{
				var existingContractHash = System.IO.File.ReadAllText("oracle-contract.txt");

				var acsResult = Client.Send<GetACSResultPayload>(NodeRPCAddress, new GetACSPayload()).Result;

				if (!acsResult.Success)
				{
					return false;
				}

				var contactDataArr = acsResult.Contracts.Where(t => Convert.ToBase64String(t.Hash) == existingContractHash).ToList();

				if (contactDataArr.Count == 0)
				{
					System.IO.File.Delete("oracle-contract.txt");
				}
				else
				{
					contractHash = contactDataArr[0].Hash;
				}
			}

			if (contractHash == null)
			{
				var @params = new ContractExamples.QuotedContracts.OracleParameters(publicKey);
				var contract = ContractExamples.QuotedContracts.oracleFactory(@params);
				var code = ContractExamples.Execution.quotedToString(contract);

				contractHash = Merkle.innerHash(Encoding.ASCII.GetBytes(code));

				var activateContractResult = Client.Send<ResultPayload>(NodeRPCAddress, new ActivateContractPayload() { Code = code, Blocks = 100 }).Result;

				if (!activateContractResult.Success)
				{
					return false;
				}

				System.IO.File.WriteAllText("oracle-contract.txt", Convert.ToBase64String(contractHash));
			}

			var contractAddress = new Wallet.core.Data.Address(contractHash, Wallet.core.Data.AddressType.Contract);

			var getOutpointsResult =
				Client.Send<GetContractPointedOutputsResultPayload>(NodeRPCAddress, new GetContractPointedOutputsPayload() { ContractHash = contractHash }).Result;

			if (!getOutpointsResult.Success)
			{
				return false;
			}

			if (getOutpointsResult.PointedOutputs.Count == 0)
			{
				var sendContractResult = Client.Send<ResultPayload>(NodeRPCAddress, new SpendPayload() { Address = contractAddress.ToString(), Amount = 1 }).Result;

				if (!sendContractResult.Success)
				{
					return false;
				}
			}

			return true;
		}
	}
}
