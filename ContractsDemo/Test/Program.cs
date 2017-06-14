using System;
using BlockChain.Data;
using Consensus;
using Infrastructure;
using Zen.RPC;
using Zen.RPC.Common;

namespace Test
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			string address = "127.0.0.1:5555";

			var getOutputResult = Client.Send<GetContractPointedOutputsResultPayload>(address, new GetContractPointedOutputsPayload()
			{
				ContractHash = new Wallet.core.Data.Address("ciHqc7XRol76SOZ5HHFdoaxG6mlbYkrIktRc2P/64B8U=").Bytes
			}).Result;

			if (getOutputResult.Success)
			{
				foreach (var item in GetContractPointedOutputsResultPayload.Unpack(getOutputResult.PointedOutputs))
				{
					Console.WriteLine(item.Item1 + " " + item.Item2);
				}
			}

			Console.WriteLine("Hello World!");
		}
	}
}
