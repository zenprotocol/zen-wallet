using System;
using Infrastructure.Testing;
using NUnit.Framework;

namespace BlockChain
{
	public class ACSTests : BlockChainContractTestsBase
	{
		byte[] compiledContract;

		string contractFsCode = @"
module Test
open Consensus.Types
let run (context : ContractContext, witnesses: Witness list, outputs: Output list, contract: ExtendedContract) = (context.utxo |> Map.toSeq |> Seq.map fst, witnesses, outputs, contract)
";

		[SetUp]
		public void Setup()
		{
			OneTimeSetUp();

			compiledContract = GetCompliedContract(contractFsCode);

			Assert.That(_BlockChain.HandleBlock(_GenesisBlock), Is.True);
		}

		private void AddToACS(UInt32 lastBlock)
		{
			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				_BlockChain.ACS.Add(dbTx, new ACSItem()
				{
					Hash = compiledContract,
					LastBlock = lastBlock,
					KalapasPerBlock = (ulong)contractFsCode.Length * 1000
				});
				dbTx.Commit();
			}

		}

		[Test]
		public void ShouldExtendContract()
		{
			ACSItem acsItem = null;
			AddToACS(_GenesisBlock.header.blockNumber + 1);

			ulong blocsToExtend = 2;

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				acsItem = _BlockChain.ACS.Get(dbTx, compiledContract).Value;
			}

			var output = Utils.GetContractSacrificeLock(compiledContract, acsItem.KalapasPerBlock * blocsToExtend);
			var tx = Utils.GetTx().AddOutput(output);
			var bk = _GenesisBlock.Child().AddTx(tx);

			Assert.That(_BlockChain.HandleBlock(bk), "Should add block", Is.True);

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				Assert.That(_BlockChain.ACS.IsActive(dbTx, compiledContract, bk.header.blockNumber), "Contract should be active", Is.True);
			}

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				var acsItemChanged = _BlockChain.ACS.Get(dbTx, compiledContract).Value;

				Assert.That(acsItemChanged.LastBlock - acsItem.LastBlock, Is.EqualTo(blocsToExtend), "Contract should be extended");
			}
		}

		[Test]
		public void ShouldNotExtendInactiveContract()
		{
			ACSItem acsItem = null;
			AddToACS(_GenesisBlock.header.blockNumber + 1);

			ulong blocsToExtend = 2;

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				acsItem = _BlockChain.ACS.Get(dbTx, compiledContract).Value;
			}

			var output = Utils.GetContractSacrificeLock(compiledContract, acsItem.KalapasPerBlock * blocsToExtend);
			var tx = Utils.GetTx().AddOutput(output);
			var bk = _GenesisBlock.Child().Child().AddTx(tx);

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				Assert.That(_BlockChain.ACS.IsActive(dbTx, compiledContract, bk.header.blockNumber), Is.False);
			}

			Assert.That(_BlockChain.HandleBlock(bk), "Should add block", Is.True);

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				Assert.That(_BlockChain.ACS.IsActive(dbTx, compiledContract, bk.header.blockNumber), Is.False);
			}

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				var acsItemChanged = _BlockChain.ACS.Get(dbTx, compiledContract).Value;

				Assert.That(acsItemChanged.LastBlock, Is.EqualTo(acsItem.LastBlock));
			}
		}
	}
}
