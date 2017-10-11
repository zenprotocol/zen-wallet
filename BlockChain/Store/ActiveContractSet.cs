using System;
using System.Collections.Generic;
using Store;
using System.Linq;
using BlockChain.Data;
using Consensus;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Collections;

namespace BlockChain
{
	using ContractFunction = FSharpFunc<Tuple<byte[], byte[], FSharpFunc<Types.Outpoint, FSharpOption<Types.Output>>>, FSharpResult<Tuple<FSharpList<Types.Outpoint>, FSharpList<Types.Output>, byte[]>, string>>;
	using ContractCostFunction = FSharpFunc<Tuple<byte[], byte[], FSharpFunc<Types.Outpoint, FSharpOption<Types.Output>>>, System.Numerics.BigInteger>;

	public class ACSItem
	{
		public byte[] Hash { get; set; }
		public byte[] CostFn { get; set; }
		public ulong KalapasPerBlock { get; set; }
		public UInt32 LastBlock { get; set; }
		public string Extracted { get; set; }
		public byte[] Serialized { get; set; }
	}

    public class ActiveContractSet : MsgPackStore<ACSItem>
    {
        private const int KALAPAS_PER_BYTE = 1;

        public ActiveContractSet() : base("contract-set") { }

        public void Add(TransactionContext dbTx, ACSItem item)
        {
            Put(dbTx, item.Hash, item);
        }

        //public HashSet Keys(TransactionContext dbTx)
        //{
        //    return new HashSet(All(dbTx).Select(t => t.Item2.Hash));
        //}

        public bool IsActive(TransactionContext dbTx, byte[] contractHash)
        {
            return ContainsKey(dbTx, contractHash);
        }

        public UInt32 LastBlock(TransactionContext dbTx, byte[] contractHash)
        {
            return Get(dbTx, contractHash).Value.LastBlock;
        }

        public bool TryExtend(TransactionContext dbTx, byte[] contractHash, ulong kalapas)
        {
            if (!IsActive(dbTx, contractHash))
            {
                return false;
            }

            var acsItem = Get(dbTx, contractHash);
            acsItem.Value.LastBlock += (uint)(kalapas / acsItem.Value.KalapasPerBlock);

            Add(dbTx, acsItem.Value);

            return true;
        }

        public static ulong KalapasPerBlock(string serializedContract)
        {
            return (ulong)serializedContract.Length * KALAPAS_PER_BYTE;
        }

        public bool TryActivate(TransactionContext dbTx, string contractCode, ulong kalapas, byte[] contractHash, uint blockNumber)
        {
            if (IsActive(dbTx, contractHash))
            {
                return false;
            }

			//TODO: module name
			var extration = ContractExamples.FStarExecution.extract(contractCode);

			if (FSharpOption<string>.get_IsNone(extration))
			{
				BlockChainTrace.Information("Could not extract contract");
				return false;
			}

			var compilation = ContractExamples.FStarExecution.compile(extration.Value);

			if (FSharpOption<byte[]>.get_IsNone(compilation))
			{
				BlockChainTrace.Information("Could not complie contract");
				return false;
			}

            var kalapasPerBlock = KalapasPerBlock(contractCode);

            if (kalapas < kalapasPerBlock)
            {
                return false;
            }

            var blocks = Convert.ToUInt32(kalapas / kalapasPerBlock);

            Add(dbTx, new ACSItem()
            {
                Hash = contractHash,
                KalapasPerBlock = kalapasPerBlock,
                LastBlock = blockNumber + blocks,
                Extracted = extration.Value,
                Serialized = compilation.Value
            });

            BlockChainTrace.Information($"Contract activated for {blocks} blocks", contractHash);

            return true;
		}

		public HashDictionary<ACSItem> GetExpiringList(TransactionContext dbTx, uint blockNumber)
		{
#if TRACE
			All(dbTx).Where(t => t.Item2.LastBlock == blockNumber).ToList().ForEach(t => BlockChainTrace.Information($"contract due to expire at {blockNumber}", t.Item1));
#endif

			var values = new HashDictionary<ACSItem>();

			foreach (var contract in All(dbTx).Where(t => t.Item2.LastBlock <= blockNumber))
			{
				values[contract.Item1] = contract.Item2;
			}

			return values;
		}

		public void DeactivateContracts(TransactionContext dbTx, IEnumerable<byte[]> list)
		{
			foreach (var item in list)
			{
				if (IsActive(dbTx, item))
					Remove(dbTx, item);
				else
					throw new Exception();
			}
		}

		public ContractFunction GetContractFunction(TransactionContext dbTx, byte[] contractHash)
		{
            var acsItem = Get(dbTx, contractHash);

            if (acsItem == null)
            {
                return null;
            }

            var deserialization = FSharpOption<Tuple<ContractFunction, ContractCostFunction>>.None;

            try
            {
                deserialization = ContractExamples.FStarExecution.deserialize(acsItem.Value.Serialized);
            }
            catch (Exception e)
            {
				BlockChainTrace.Error("Error deserializing contract", e);
			}

            if (FSharpOption<Tuple<ContractFunction, ContractCostFunction>>.get_IsNone(deserialization) || deserialization == null)
            {
                BlockChainTrace.Information("Reserializing contract");

                try
                {
                    var compilation = ContractExamples.FStarExecution.compile(acsItem.Value.Extracted);

                    if (FSharpOption<byte[]>.get_IsNone(compilation))
                    {
                        return null;
                    }

                    acsItem.Value.Serialized = compilation.Value;

                    Add(dbTx, acsItem.Value);

                    deserialization = ContractExamples.FStarExecution.deserialize(compilation.Value);

					if (FSharpOption<Tuple<ContractFunction, ContractCostFunction>>.get_IsNone(deserialization))
					{
						BlockChainTrace.Error("Error deserializing contract");
						return null;
					}
				}
				catch (Exception e)
				{
					BlockChainTrace.Error("Error recompiling contract", e);
					return null;
				}
			}

			return deserialization.Value.Item1;
		}
	}
}