using System;

namespace RPC.Data
{
	public class SendContractPayload
	{
		public byte[] ContractHash { get; set; }
		public byte[] Data { get; set; }
	}
}