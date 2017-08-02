﻿using System;
using System.Collections.Generic;
using Store;
using System.Linq;
using BlockChain.Data;
using System.Text;
using Consensus;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Collections;
using System.Reflection;

namespace BlockChain
{
	using TransactionSkeleton = Tuple<FSharpList<Types.Outpoint>, FSharpList<Types.Output>, byte[]>;
	using ContractFunction = FSharpFunc<Tuple<byte[], byte[], FSharpFunc<Types.Outpoint, FSharpOption<Types.Output>>>, Tuple<FSharpList<Types.Outpoint>, FSharpList<Types.Output>, byte[]>>;

	public class ACSItem
	{
		public byte[] Hash { get; set; }
        public string Code { get; set; }
		public byte[] CostFn { get; set; }
		public ulong KalapasPerBlock { get; set; }
		public UInt32 LastBlock { get; set; }
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

            //  string fsharpCode;
            //  ContractHelper.Extract(contractCode, out fsharpCode);
            //	var fsharpCode = new StrongBox<byte[]>();
            //	return ContractHelper.Extract(contractCode, fsharpCode).ContinueWith(t => {

            try
            {
                var compiledCodeOpt = ContractExamples.Execution.compile(contractCode);

                if (FSharpOption<MethodInfo>.get_IsNone(compiledCodeOpt))
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                //TODO: print the exception on Info rather than Error, or don't print the exception at all
                BlockChainTrace.Error("Error compiling contract", e);
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
                Code = contractCode
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

			try
			{
                var methodOpt = ContractExamples.Execution.compile(acsItem.Value.Code);

                if (FSharpOption<MethodInfo>.get_IsNone(methodOpt))
				{
					return null;
				}

                return ContractFunction.FromConverter(t =>
				{
					return (TransactionSkeleton)methodOpt.Value.Invoke(null, new object[] { t.Item1, t.Item2, t.Item3 });
				});
			}
			catch (Exception e)
			{
				//TODO: print the exception on Info rather than Error, or don't print the exception at all
				BlockChainTrace.Error("Error compiling contract", e);
				return null;
			}
		}
	}
}