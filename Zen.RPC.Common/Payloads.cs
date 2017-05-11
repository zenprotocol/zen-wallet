namespace Zen.RPC.Common
{
	public class BasePayload
	{
		public string Action { get; set; }
	}

	public class SendContractPayload : BasePayload
	{
		public byte[] ContractHash { get; set; }
		public byte[] Data { get; set; }
	}

	public class ResultPayload
	{
		public bool Success { get; set; }
		public string Message { get; set; }
	}

	public class GetACSResultPayload : ResultPayload
	{
		public byte[][] Contracts { get; set; }
	}
}