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

	public class ResultPayload
	{
		public bool Success { get; set; }
		public string Message { get; set; }
	}

	public class HelloPayload : BasePayload
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

	public class GetACSPayload : BasePayload
	{
	}

	public class ContractData
	{
		public byte[] Hash { get; set; }
		public uint LastBlock { get; set; }
		public byte[] Code { get; set; }
	}

	public class GetACSResultPayload : ResultPayload
	{
		public ContractData[] Contracts { get; set; }	
	}

	public class GetContractCodePayload : BasePayload
	{
		public byte[] Hash { get; set; }
	}

	public class GetContractCodeResultPayload : ResultPayload
	{
		public byte[] Code { get; set; }
	}

	public class GetContractTotalAssetsPayload : BasePayload
	{
		public byte[] Hash { get; set; }
	}

	public class GetContractTotalAssetsResultPayload : ResultPayload
	{
		public ulong Confirmed { get; set; }
		public ulong Unconfirmed { get; set; }
	}

	public class GetOutpointPayload : BasePayload
	{
		public byte[] Asset { get; set; }
		public byte[] Address { get; set; }
		public bool IsContract { get; set; }
		public bool IsPK { get; set; }
	}

	public class GetOutpointResultPayload : ResultPayload
	{
		public byte[] TXHash { get; set; }
		public uint Index { get; set; }
	}

	public class GetPointedOutpointPayload : BasePayload
	{
		public byte[] Asset { get; set; }
		public byte[] Address { get; set; }
		public bool IsContract { get; set; }
		public bool IsPK { get; set; }
	}

	public class GetPointedOutpointResultPayload : ResultPayload
	{
		public Consensus.Types.Output Output { get; set; }
		public Consensus.Types.Outpoint Outpoint { get; set; }
	}

    public class SendContractPayload : BasePayload
	{
		public byte[] ContractHash { get; set; }
		public byte[] Data { get; set; }
	}

	public class MakeTransactionPayload : BasePayload
	{
		public byte[] Asset { get; set; }
		public string Address { get; set; }
		public ulong Amount { get; set; }
	}
}