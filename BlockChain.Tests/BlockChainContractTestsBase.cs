using System;
using System.Collections.Generic;
using Consensus;
using Microsoft.FSharp.Collections;
using NUnit.Framework;
using Wallet.core.Data;
using System.Linq;

namespace BlockChain
{
	public class BlockChainContractTestsBase : BlockChainTestsBase
	{
		protected byte[] GetCompliedContract(string fsCode)
		{
			byte[] compiledContract;

			Assert.That(ContractHelper.Compile(fsCode, out compiledContract), "Should compile", Is.True);

			return compiledContract;
		}

		protected Types.Transaction ExecuteContract(byte[] compiledContract)
		{
			var output = Utils.GetOutput(Key.Create().Address, Consensus.Tests.zhash, 10);

			var outputs = new List<Types.Output>();

			outputs.Add(output);

			var utxos = new List<Tuple<Types.Outpoint, Types.Output>>();

			using (var context = _BlockChain.GetDBTransaction())
			{
				foreach (var item in _BlockChain.UTXOStore.All(context, null, false).Where(t => t.Value.@lock is Types.OutputLock.ContractLock))
				{
					utxos.Add(new Tuple<Types.Outpoint, Types.Output>(item.Key, item.Value));
				}
			}

			Types.Transaction contractCreatedTransaction;
			Assert.That(ContractHelper.Execute(compiledContract, out contractCreatedTransaction, new ContractArgs()
			{
				context = new Types.ContractContext(compiledContract, new FSharpMap<Types.Outpoint, Types.Output>(utxos)),
				witnesses = new List<byte[]>(),
				outputs = outputs,
				option = Types.ExtendedContract.NewContract(new Types.Contract(new byte[] { }, new byte[] { }, new byte[] { }))
			}), "Should execute", Is.True);

			return contractCreatedTransaction;
		}

		protected void AddToACS(byte[] compiledContract, string contractFsCode, UInt32 lastBlock)
		{
			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				new ActiveContractSet().Add(dbTx, new ACSItem()
				{
					Hash = compiledContract,
					LastBlock = lastBlock,
					KalapasPerBlock = (ulong)contractFsCode.Length * 1000
				});
				dbTx.Commit();
			}
		}
	}
}