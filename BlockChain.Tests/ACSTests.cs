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

			var contractLockOutput = Utils.GetContractOutput(compiledContract, null, Consensus.Tests.zhash, 100);
			var tx = Utils.GetTx().AddOutput(contractLockOutput);

			Assert.That(_BlockChain.HandleBlock(_GenesisBlock.AddTx(tx)), Is.True);
		}

		void AddToACS(UInt32 lastBlock)
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

		[Test]
		public void ShouldExpireAfterOneBlock()
		{
			AddToACS(_GenesisBlock.header.blockNumber + 1);

			var bk = _GenesisBlock;

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				Assert.That(new ActiveContractSet().IsActive(dbTx, compiledContract), Is.True, "Contract should be active");
			}

			bk = bk.Child();
			_BlockChain.HandleBlock(bk);

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				Assert.That(new ActiveContractSet().IsActive(dbTx, compiledContract), Is.False, "Contract should not be active");
			}
		}

		[Test]
		public void ShouldExpireAfterTwoBlocks()
		{
			AddToACS(_GenesisBlock.header.blockNumber + 2);

			var bk = _GenesisBlock;

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				Assert.That(new ActiveContractSet().IsActive(dbTx, compiledContract), Is.True, "Contract should be active");
			}

			bk = bk.Child();
			_BlockChain.HandleBlock(bk);

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				Assert.That(new ActiveContractSet().IsActive(dbTx, compiledContract), Is.True, "Contract should remain active");
			}

			bk = bk.Child();
			_BlockChain.HandleBlock(bk);

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				Assert.That(new ActiveContractSet().IsActive(dbTx, compiledContract), Is.False, "Contract should not be active");
			}
		}

		[Test]
		public void ShouldExtendContract()
		{
			ACSItem acsItem = null;
			AddToACS(_GenesisBlock.header.blockNumber + 1);

			ulong blocksToExtend = 2;

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				acsItem = new ActiveContractSet().Get(dbTx, compiledContract).Value;
			}

			var output = Utils.GetContractSacrificeLock(compiledContract, acsItem.KalapasPerBlock * blocksToExtend);
			var tx = Utils.GetTx().AddOutput(output);
			var bk = _GenesisBlock.Child().AddTx(tx);

			Assert.That(_BlockChain.HandleBlock(bk), Is.True, "Should add block");

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				Assert.That(new ActiveContractSet().IsActive(dbTx, compiledContract), Is.True, "Contract should be active");
			}

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				var acsItemChanged = new ActiveContractSet().Get(dbTx, compiledContract).Value;

				Assert.That(acsItemChanged.LastBlock - acsItem.LastBlock, Is.EqualTo(blocksToExtend), "Contract should be extended");
			}
		}

		[Test]
		public void ShouldNotExtendInactiveContract()
		{
			ACSItem acsItem = null;
			AddToACS(_GenesisBlock.header.blockNumber + 1);

			ulong blocksToExtend = 2;

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				acsItem = new ActiveContractSet().Get(dbTx, compiledContract).Value;
			}

			var output = Utils.GetContractSacrificeLock(compiledContract, acsItem.KalapasPerBlock * blocksToExtend);
			var tx = Utils.GetTx().AddOutput(output);
			var bk = _GenesisBlock.Child().Child().AddTx(tx);

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				Assert.That(new ActiveContractSet().IsActive(dbTx, compiledContract), Is.False);
			}

			Assert.That(_BlockChain.HandleBlock(bk), Is.True, "Should add block");

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				Assert.That(new ActiveContractSet().IsActive(dbTx, compiledContract), Is.False);
			}

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				var acsItemChanged = new ActiveContractSet().Get(dbTx, compiledContract).Value;

				Assert.That(acsItemChanged.LastBlock, Is.EqualTo(acsItem.LastBlock));
			}
		}

		[Test]
		public void ShouldAcceptTxGenereatedByActiveContract()
		{
			AddToACS(_GenesisBlock.header.blockNumber + 1);

			var tx = ExecuteContract(compiledContract);

			Assert.That(_BlockChain.HandleBlock(_GenesisBlock.Child().AddTx(tx)), Is.False);
		}

		[Test]
		public void ShouldRejectTxGenereatedByInactiveContract()
		{
			AddToACS(_GenesisBlock.header.blockNumber);

			var tx = ExecuteContract(compiledContract);

			Assert.That(_BlockChain.HandleBlock(_GenesisBlock.Child().AddTx(tx)), Is.False);
		}

		[Test]
		public void ShouldUndoExtendOnReorder()
		{
			ACSItem acsItem = null;
			AddToACS(_GenesisBlock.header.blockNumber + 1);

			ulong blocksToExtend = 20;

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				acsItem = new ActiveContractSet().Get(dbTx, compiledContract).Value;
			}

			var output = Utils.GetContractSacrificeLock(compiledContract, acsItem.KalapasPerBlock * blocksToExtend);
			var tx = Utils.GetTx().AddOutput(output);
			var bk = _GenesisBlock.Child().AddTx(tx);

			Assert.That(_BlockChain.HandleBlock(bk), "Should add block", Is.True);

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				Assert.That(new ActiveContractSet().IsActive(dbTx, compiledContract), "Contract should be active", Is.True);
			}

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				var acsItemChanged = new ActiveContractSet().Get(dbTx, compiledContract).Value;

				Assert.That(acsItemChanged.LastBlock - acsItem.LastBlock, Is.EqualTo(blocksToExtend), "Contract should be extended");
			}

			var child = _GenesisBlock.Child();
			_BlockChain.HandleBlock(child);
			child = child.Child();
			_BlockChain.HandleBlock(child); // cause reorder

			using (var dbTx = _BlockChain.GetDBTransaction())
			{
				var acsItemChanged = new ActiveContractSet().Get(dbTx, compiledContract).Value;
				Assert.That(acsItemChanged.LastBlock, Is.EqualTo(acsItem.LastBlock));
			}
		}

			//var tx = ExecuteContract(compiledContract);

			//var child = _GenesisBlock.Child();
			//var orphan = child.Child().AddTx(tx);

			//Assert.That(_BlockChain.HandleBlock(orphan), Is.True);
			//Assert.That(_BlockChain.HandleBlock(child), Is.True); // cause an undo


	}
}
