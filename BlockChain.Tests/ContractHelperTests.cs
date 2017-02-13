using System;
using NUnit.Framework;
using Infrastructure.Testing;
using System.Text;
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
	//	List<Types.Outpoint> inputs = new List<Types.Outpoint>();

		//[OneTimeSetUp]
		//public void OneTimeSetUp()
		//{
		//	base.OneTimeSetUp();
		//}

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
					byte[] txHash = new byte[item.Key.Length - 1];
					Array.Copy(item.Key, txHash, txHash.Length);
					var index = Convert.ToUInt32(item.Key[item.Key.Length - 1]);

					utxos.Add(new Tuple<Types.Outpoint, Types.Output>(new Types.Outpoint(txHash, index), item.Value));
				}
			}

			Assert.That(ContractHelper.Execute(compiledContract, out contractCreatedTransaction, new ContractArgs()
			{
				context = new Types.ContractContext(compiledContract, new FSharpMap<Types.Outpoint, Types.Output>(utxos)),
		//		inputs = inputs,
				witnesses = new List<byte[]>(),
				outputs = outputs,
				option = Types.ExtendedContract.NewContract(new Types.Contract(new byte[] { },new byte[] { },new byte[] { }))
			}), "Should execute", Is.True);
		}

		[Test, Order(3)]
		public void ShouldValidate()
		{
			CollectionAssert.AreEqual(contractCreatedTransaction.outputs, outputs);
		}

		[Test, Order(4)]
		public void ShouldValidateInMempool()
		{
			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				_BlockChain.ActiveContractSet.Add(dbTx, compiledContract, 1000);
				dbTx.Commit();
			}

			Assert.That(_BlockChain.HandleTransaction(contractCreatedTransaction), "Should add block", Is.True);
		}

		[Test, Order(5)]
		public void ShouldValidateInBlock()
		{
			var bk = _GenesisBlock.Child().AddTx(contractCreatedTransaction);

			Assert.That(_BlockChain.HandleBlock(bk), "Should add block", Is.False);
		}

		[Test, Order(6)]
		public void ShouldActivateContract()
		{
		}

		[Test, Order(7)]
		public void ShouldExtendContract()
		{
			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				_BlockChain.ActiveContractSet.Add(dbTx, compiledContract, 1000);
				dbTx.Commit();
			}

			var output = Utils.GetContractOutput(compiledContract, null, null, 12);
			var tx = Utils.GetTx().AddOutput(output);

			////
			var bk = _GenesisBlock.Child().AddTx(contractCreatedTransaction);

			Assert.That(_BlockChain.HandleTransaction(tx), "Should accept tx", Is.True);
		}


		[Test, Order(7)]
		public void ShouldExtendMempoolContract()
		{
			var output = Utils.GetContractOutput(compiledContract, null, null, 12);
			var tx = Utils.GetTx().AddOutput(output);

			////
			var bk = _GenesisBlock.Child().AddTx(contractCreatedTransaction);

			Assert.That(_BlockChain.HandleTransaction(tx), "Should accept tx", Is.True);
		}

		[Test, Order(8)]
		public void ShouldNotExtendInvalidContract()
		{

		}

		//TODO: test peer banning

	}
}
