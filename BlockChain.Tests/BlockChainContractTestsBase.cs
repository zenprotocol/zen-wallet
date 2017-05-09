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

			Types.Transaction contractCreatedTransaction;
			Assert.That(ContractHelper.Execute(out contractCreatedTransaction, new ContractArgs()
			{
				ContractHash = compiledContract,
				Utxos = utxos
			}), Is.True);

			return contractCreatedTransaction;
		}

		protected void AddToACS(byte[] compiledContract, string contractCode, UInt32 lastBlock)
		{
			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				new ActiveContractSet().Add(dbTx, new ACSItem()
				{
					Hash = compiledContract,
					LastBlock = lastBlock,
					KalapasPerBlock = (ulong)contractCode.Length * 1000
				});
				dbTx.Commit();
			}
		}
	}
}