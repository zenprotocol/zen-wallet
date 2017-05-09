using System;
using NUnit.Framework;
using System.Linq;
using Consensus;
using Wallet.core.Data;
using System.Collections.Generic;
using Microsoft.FSharp.Collections;

namespace BlockChain
{
	public class ContractHelperTests : BlockChainTestsBase
	{
		byte[] compiledContract;
		Types.Transaction contractCreatedTransaction;
		List<Types.Output> outputs = new List<Types.Output>();

		[Test, Order(1)]
		public void ShouldCompile()
		{
#if CSHARP_CONTRACTS
			string contractCode = @"
    using System;
    using System.Collections.Generic;
	using static Consensus.Types;
    using Microsoft.FSharp.Core;

    public class Test
    {
        public static Tuple<IEnumerable<Outpoint>, IEnumerable<Output>, FSharpOption<ExtendedContract>> run(
			byte[] contractHash,
			SortedDictionary<Outpoint, Output> utxos,
			byte[] message
		) {
			var outpoints = new List<Outpoint>();
			foreach (var item in utxos)
			{
				outpoints.Add(item.Key);
			}

			var outputs = new List<Output>();
			foreach (var item in utxos)
			{
				outputs.Add(item.Value);
			}

			return new Tuple<IEnumerable<Outpoint>, IEnumerable<Output>, FSharpOption<ExtendedContract>>(
				outpoints, outputs, FSharpOption<ExtendedContract>.None
			);
        }
    }";
#else
			var contractCode = @"
module Test
open Consensus.Types
let run (hash : byte[], utxos: Map<Outpoint, Output>, message: byte[]) = (utxos |> Map.toSeq |> Seq.map fst, utxos |> Map.toSeq |> Seq.map snd, Option<ExtendedContract>.None)
";
#endif
			Assert.That(ContractHelper.Compile(contractCode, out compiledContract), "Should compile", Is.True);

			var contractOutput1 = Utils.GetContractOutput(compiledContract, new byte[] { }, Consensus.Tests.zhash, 11);
			var contractOutput2 = Utils.GetContractOutput(compiledContract, new byte[] { }, Consensus.Tests.zhash, 12);
			var contractOutput3 = Utils.GetContractOutput(compiledContract, new byte[] { }, Consensus.Tests.zhash, 13);

			var bk = _GenesisBlock.AddTx(Utils.GetTx().AddOutput(contractOutput1).AddOutput(contractOutput2).AddOutput(contractOutput3));

			Assert.That(_BlockChain.HandleBlock(bk), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));
		}

		[Test, Order(2)]
		public void ShouldExecute()
		{
			var output = Utils.GetOutput(Key.Create().Address, new byte[] { }, 10);

			outputs.Add(output);

			var utxos = new SortedDictionary<Types.Outpoint, Types.Output>();

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				foreach (var item in _BlockChain.UTXOStore.All(dbTx, null, false).Where(t =>
				{
					var contractLock = t.Item2.@lock as Types.OutputLock.ContractLock;
					return contractLock != null && contractLock.contractHash.SequenceEqual(compiledContract);
				}))
				{
					utxos[item.Item1] = item.Item2;
				}
			}

			Assert.That(ContractHelper.Execute(out contractCreatedTransaction, new ContractArgs()
			{
				ContractHash = compiledContract,
				Utxos = utxos
			}), Is.True);
		}

		[Test, Order(3)]
		public void ShouldPassVerification()
		{
			var utxos = new List<Tuple<Types.Outpoint, Types.Output>>();

			using (var context = _BlockChain.GetDBTransaction())
			{
				foreach (var item in _BlockChain.UTXOStore.All(context, null, false))
				{
					utxos.Add(new Tuple<Types.Outpoint, Types.Output>(item.Item1, item.Item2));
				}
			}

			var ptx = TransactionValidation.toPointedTransaction(contractCreatedTransaction, ListModule.OfSeq(utxos.Select(t=>t.Item2)));
			byte[] _contractHash;
			Assert.That(BlockChain.IsContractGeneratedTx(ptx, out _contractHash), Is.EqualTo(BlockChain.IsContractGeneratedTxResult.ContractGenerated));
			using (var context = _BlockChain.GetDBTransaction())
			{
				Assert.That(_BlockChain.IsValidTransaction(context, ptx, _contractHash), Is.True);
			}
        }
	}
}
