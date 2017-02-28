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
			var fs = @"
module Test
open Consensus.Types
let run (context : ContractContext, witnesses: Witness list, outputs: Output list, contract: ExtendedContract) = (context.utxo |> Map.toSeq |> Seq.map fst, witnesses, outputs, contract)
";

			Assert.That(ContractHelper.Compile(fs, out compiledContract), "Should compile", Is.True);

			var contractOutput1 = Utils.GetContractOutput(compiledContract, new byte[] { }, Consensus.Tests.zhash, 11);
			var contractOutput2 = Utils.GetContractOutput(compiledContract, new byte[] { }, Consensus.Tests.zhash, 12);
			var contractOutput3 = Utils.GetContractOutput(compiledContract, new byte[] { }, Consensus.Tests.zhash, 13);

			var bk = _GenesisBlock.AddTx(Utils.GetTx().AddOutput(contractOutput1).AddOutput(contractOutput2).AddOutput(contractOutput3));

			Assert.That(_BlockChain.HandleBlock(bk), Is.True);
		}

		[Test, Order(2)]
		public void ShouldExecute()
		{
			var output = Utils.GetOutput(Key.Create().Address, new byte[] { }, 10);

			outputs.Add(output);

			var utxos = new List<Tuple<Types.Outpoint, Types.Output>>();

			using (var context = _BlockChain.GetDBTransaction())
			{
				foreach (var item in _BlockChain.UTXOStore.All(context, null, false))
				{
					utxos.Add(new Tuple<Types.Outpoint, Types.Output>(item.Key, item.Value));
				}
			}

			Assert.That(ContractHelper.Execute(compiledContract, out contractCreatedTransaction, new ContractArgs()
			{
				context = new Types.ContractContext(compiledContract, new FSharpMap<Types.Outpoint, Types.Output>(utxos)),
				witnesses = new List<byte[]>(),
				outputs = outputs,
				option = Types.ExtendedContract.NewContract(new Types.Contract(new byte[] { },new byte[] { },new byte[] { }))
			}), "Should execute", Is.True);
		}

		[Test, Order(3)]
		public void ShouldPassVerification()
		{
			var utxos = new List<Tuple<Types.Outpoint, Types.Output>>();

			using (var context = _BlockChain.GetDBTransaction())
			{
				foreach (var item in _BlockChain.UTXOStore.All(context, null, false))
				{
					utxos.Add(new Tuple<Types.Outpoint, Types.Output>(item.Key, item.Value));
				}
			}

			var ptx = TransactionValidation.toPointedTransaction(contractCreatedTransaction, ListModule.OfSeq(utxos.Select(t=>t.Item2)));
			byte[] _contractHash;
			Assert.That(BlockChain.IsContractGeneratedTx(ptx, out _contractHash), Is.EqualTo(BlockChain.IsContractGeneratedTxResult.ContractGenerated));
			using (var context = _BlockChain.GetDBTransaction())
			{
				Assert.That(BlockChain.IsContractGeneratedTransactionValid(context, ptx, _contractHash), Is.True);
			}
        }
	}
}
