using System;

namespace Zen.RPC.Common
{
	public class BasePayload
	{
		public Type Type { get; set; }
		public BasePayload()
		{
			Type = this.GetType();
		}
	}

    public class HelloPayload : BasePayload
	{
     
	}

	public class GetACSPayload : BasePayload
	{

	}
	
	public class HelloResultPayload : ResultPayload
	{
		public HelloResultPayload()
		{
			Success = true;
			Message = "Hello! it's " + DateTime.Now.ToLongTimeString();
		}
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