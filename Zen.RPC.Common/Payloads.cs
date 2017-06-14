using System;
using System.Collections.Generic;
using Consensus;
using System.Linq;

namespace Zen.RPC.Common
{
	public class BasePayload
	{
		public Type Type { get; set; }
		public BasePayload()
		{
			Type = this.GetType();
		}

		public override string ToString()
		{
			return Type.Name;
		}
	}

	public class ResultPayload
	{
		public bool Success { get; set; }
		public string Message { get; set; }

		public override string ToString()
		{
			var status = Success ? "" : "Failure";
			var str = $"{this.GetType().Name} {status}";

			if (Message != null)
				str += $" Message={Message}";

			return str;
		}
	}

	public class HelloPayload : BasePayload
	{
	}

	public class HelloResultPayload : ResultPayload
	{
		public HelloResultPayload()
		{
			Success = true;
			Message = "Hello! it's " + DateTime.Now.ToUniversalTime().ToLongTimeString();
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

		public override string ToString()
		{
			var count = Contracts == null ? "none" : Contracts.Length.ToString();
			return base.ToString() + $" {count} Contracts";
		}
	}

	//public class GetContractCodePayload : BasePayload
	//{
	//	public byte[] Hash { get; set; }
	//}

	//public class GetContractCodeResultPayload : ResultPayload
	//{
	//	public byte[] Code { get; set; }
	//}

	public class GetContractTotalAssetsPayload : BasePayload
	{
		public byte[] Hash { get; set; }
	}

	public class GetContractTotalAssetsResultPayload : ResultPayload
	{
		public ulong Confirmed { get; set; }
		public ulong Unconfirmed { get; set; }
	}

	public class GetContractPointedOutputsPayload : BasePayload
	{
		public byte[] ContractHash { get; set; }
	}

	public class GetContractPointedOutputsResultPayload : ResultPayload
	{
		public List<Tuple<byte[], byte[]>> PointedOutputs { get; set; }

		public override string ToString()
		{
			var count = PointedOutputs == null ? "none" : PointedOutputs.Count().ToString();
			return base.ToString() + $" {count} PointedOutputs";
		}

		static readonly MsgPack.Serialization.MessagePackSerializer<Types.Outpoint> outpointSerializer = 
					Serialization.context.GetSerializer<Types.Outpoint>();
		static readonly MsgPack.Serialization.MessagePackSerializer<Types.Output> outputSerializer =
					Serialization.context.GetSerializer<Types.Output>();

		public static List<Tuple<byte[], byte[]>> Pack(List<Tuple<Types.Outpoint, Types.Output>> list)
		{
			return new List<Tuple<byte[], byte[]>>(list.Select(
				t => new Tuple<byte[], byte[]>(
					outpointSerializer.PackSingleObject(t.Item1),
					outputSerializer.PackSingleObject(t.Item2)
				)));
		}

		public static IEnumerable<Tuple<Types.Outpoint, Types.Output>> Unpack(List<Tuple<byte[], byte[]>> list)
		{
            return list.Select(t => new Tuple<Types.Outpoint, Types.Output>(
                outpointSerializer.UnpackSingleObject(t.Item1),
                outputSerializer.UnpackSingleObject(t.Item2))
            );
        }
	}

    public class SendContractPayload : BasePayload
	{
		public byte[] ContractHash { get; set; }
		public byte[] Data { get; set; }
	}

	public class ActivateContractPayload : BasePayload
	{
		public string Code { get; set; }
		public int Blocks { get; set; }
	}

	public class SpendPayload : BasePayload
	{
		public string Address { get; set; }
		public ulong Amount { get; set; }
	}

	public class MakeTransactionPayload : BasePayload
	{
		public byte[] Asset { get; set; }
		public string Address { get; set; }
		public ulong Amount { get; set; }
	}
}