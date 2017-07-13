//using System;
//using System.Text;
//using BlockChain.Data;
//using NUnit.Framework;

//namespace BlockChain
//{
//	public class ACSTests : BlockChainContractTestsBase
//	{
//		byte[] compiledCode;
//		string code;

//        bool IsContractActive(byte[] contractHash)
//        {
//            return new GetIsContractActiveAction(contractHash).Publish().Result;
//		}

//		[SetUp]
//		public void Setup()
//		{
//			OneTimeSetUp();

//            GetTestContract(out compiledCode, out code);
//			BlockChainTrace.SetTag(compiledCode, "Contract");
//			var contractLockOutput = Utils.GetContractOutput(compiledCode, null, Consensus.Tests.zhash, 100);
//			var tx = Utils.GetTx().AddOutput(contractLockOutput).Tag("Tx");

//			Assert.That(HandleBlock(_GenesisBlock.AddTx(tx).Tag("Genesis")), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));
//		}

//		[Test]
//		public void ShouldExpireAfterOneBlock()
//		{
//			AddToACS(compiledCode, code, _GenesisBlock.header.blockNumber + 1);

//			var bk = _GenesisBlock;

//			using (var dbTx = _BlockChain.GetDBTransaction())
//			{
//				Assert.That(new ActiveContractSet().IsActive(dbTx, compiledContract), Is.True, "Contract should be active");
//			}

//			bk = bk.Child();
//			HandleBlock(bk);

//			using (var dbTx = _BlockChain.GetDBTransaction())
//			{
//				Assert.That(new ActiveContractSet().IsActive(dbTx, compiledContract), Is.False, "Contract should not be active");
//			}
//		}

//		[Test]
//		public void ShouldExpireAfterTwoBlocks()
//		{
//			AddToACS(compiledContract, contractCode, _GenesisBlock.header.blockNumber + 2);

//			var bk = _GenesisBlock;

//			using (var dbTx = _BlockChain.GetDBTransaction())
//			{
//				Assert.That(new ActiveContractSet().IsActive(dbTx, compiledContract), Is.True, "Contract should be active");
//			}

//			bk = bk.Child();
//			HandleBlock(bk);

//			using (var dbTx = _BlockChain.GetDBTransaction())
//			{
//				Assert.That(new ActiveContractSet().IsActive(dbTx, compiledContract), Is.True, "Contract should remain active");
//			}

//			bk = bk.Child();
//			HandleBlock(bk);

//			using (var dbTx = _BlockChain.GetDBTransaction())
//			{
//				Assert.That(new ActiveContractSet().IsActive(dbTx, compiledContract), Is.False, "Contract should not be active");
//			}
//		}

//		[Test]
//		public void ShouldExtendContract()
//		{
//			ACSItem acsItem = null;
//			AddToACS(compiledContract, contractCode, _GenesisBlock.header.blockNumber + 1);

//			ulong blocksToExtend = 2;

//			using (var dbTx = _BlockChain.GetDBTransaction())
//			{
//				acsItem = new ActiveContractSet().Get(dbTx, compiledContract).Value;
//			}

//			var output = Utils.GetContractSacrificeLock(compiledContract, acsItem.KalapasPerBlock * blocksToExtend);
//			var tx = Utils.GetTx().AddOutput(output);
//			var bk = _GenesisBlock.Child().AddTx(tx);

//			Assert.That(HandleBlock(bk), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted), "Should add block");

//			using (var dbTx = _BlockChain.GetDBTransaction())
//			{
//				Assert.That(new ActiveContractSet().IsActive(dbTx, compiledContract), Is.True, "Contract should be active");
//			}

//			using (var dbTx = _BlockChain.GetDBTransaction())
//			{
//				var acsItemChanged = new ActiveContractSet().Get(dbTx, compiledContract).Value;

//				Assert.That(acsItemChanged.LastBlock - acsItem.LastBlock, Is.EqualTo(blocksToExtend), "Contract should be extended");
//			}
//		}

//		[Test]
//		public void ShouldNotExtendInactiveContract()
//		{
//			ACSItem acsItem = null;
//			AddToACS(compiledContract, contractCode, _GenesisBlock.header.blockNumber + 1);

//			ulong blocksToExtend = 2;

//			using (var dbTx = _BlockChain.GetDBTransaction())
//			{
//				acsItem = new ActiveContractSet().Get(dbTx, compiledContract).Value;
//			}

//			var output = Utils.GetContractSacrificeLock(compiledContract, acsItem.KalapasPerBlock * blocksToExtend);
//			var tx = Utils.GetTx().AddOutput(output);

//			using (var dbTx = _BlockChain.GetDBTransaction())
//			{
//				Assert.That(new ActiveContractSet().IsActive(dbTx, compiledContract), Is.True, "Should be active");
//			}

//			var bk = _GenesisBlock.Child();
//			HandleBlock(bk);

//			using (var dbTx = _BlockChain.GetDBTransaction())
//			{
//				Assert.That(new ActiveContractSet().IsActive(dbTx, compiledContract), Is.False, "Should be inactive");
//			}

//			HandleBlock(bk.Child().AddTx(tx));

//			using (var dbTx = _BlockChain.GetDBTransaction())
//			{
//				Assert.That(new ActiveContractSet().IsActive(dbTx, compiledContract), Is.False, "Should be inactive");
//			}

//			using (var dbTx = _BlockChain.GetDBTransaction())
//			{

//				Assert.That(new ActiveContractSet().Get(dbTx, compiledContract), Is.Null);
//			}
//		}

//		[Test]
//		public void ShouldAcceptTxGenereatedByActiveContract()
//		{
//			AddToACS(compiledContract, contractCode, _GenesisBlock.header.blockNumber + 1);

//			var tx = ExecuteContract(compiledContract);

//			Assert.That(HandleBlock(_GenesisBlock.Child().AddTx(tx)), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));
//		}

//		[Test]
//		public void ShouldAcceptTxGenereatedByActiveContract2()
//		{
//			AddToACS(compiledContract, contractCode, _GenesisBlock.header.blockNumber + 2);

//			var tx = ExecuteContract(compiledContract);

//			var bk = _GenesisBlock.Child();
//			Assert.That(HandleBlock(bk), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));
//			Assert.That(HandleBlock(bk.Child().AddTx(tx)), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));
//		}

//		[Test]
//		public void ShouldBeOrphanOfInactiveContract()
//		{
//			AddToACS(compiledContract, contractCode, _GenesisBlock.header.blockNumber);
//			BlockChainTrace.SetTag(compiledContract, "contract");
//			var tx = ExecuteContract(compiledContract).Tag("tx");

//			HandleBlock(_GenesisBlock.Child());
//			Assert.That(_BlockChain.HandleTransaction(tx), Is.EqualTo(BlockChain.TxResultEnum.OrphanIC));
//		}

//		[Test]
//		public void ShouldUndoExtendOnReorder()
//		{
//			ACSItem acsItem = null;
//			AddToACS(compiledContract, contractCode, _GenesisBlock.header.blockNumber + 1);

//			ulong blocksToExtend = 20;

//			using (var dbTx = _BlockChain.GetDBTransaction())
//			{
//				acsItem = new ActiveContractSet().Get(dbTx, compiledContract).Value;
//			}

//			var output = Utils.GetContractSacrificeLock(compiledContract, acsItem.KalapasPerBlock * blocksToExtend);
//			var tx = Utils.GetTx().AddOutput(output);
//			var bk = _GenesisBlock.Child().AddTx(tx);

//			Assert.That(HandleBlock(bk), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted), "Should add block");

//			using (var dbTx = _BlockChain.GetDBTransaction())
//			{
//				Assert.That(new ActiveContractSet().IsActive(dbTx, compiledContract), Is.True, "Contract should be active");
//			}

//			using (var dbTx = _BlockChain.GetDBTransaction())
//			{
//				var acsItemChanged = new ActiveContractSet().Get(dbTx, compiledContract).Value;

//				Assert.That(acsItemChanged.LastBlock - acsItem.LastBlock, Is.EqualTo(blocksToExtend), "Contract should be extended");
//			}

//			var child = _GenesisBlock.Child();
//			HandleBlock(child);
//			child = child.Child();
//			HandleBlock(child); // cause reorder

//			using (var dbTx = _BlockChain.GetDBTransaction())
//			{
//				Assert.That(new ActiveContractSet().Get(dbTx, compiledContract), Is.Null);
//			}
//		}

//		//var tx = ExecuteContract(compiledContract);

//		//var child = _GenesisBlock.Child();
//		//var orphan = child.Child().AddTx(tx);

//		//Assert.That(HandleBlock(orphan), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));
//		//Assert.That(HandleBlock(child), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted)); // cause an undo

//		[Test]
//		public void ShouldNotActivateUnderSacrificedContract()
//		{
//			var kalapasPerBlock = (ulong)contractCode.Length * 1000;
//			var tx = Utils.GetTx().AddOutput(Utils.GetContractSacrificeLock(new byte[] { }, kalapasPerBlock)).SetContract(
//				new Consensus.Types.Contract(Encoding.ASCII.GetBytes(contractCode), new byte[] { }, new byte[] { }));

//			Assert.That(HandleBlock(_GenesisBlock.Child().AddTx(tx)), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));

//			using (var dbTx = _BlockChain.GetDBTransaction())
//			{
//				Assert.That(new ActiveContractSet().IsActive(dbTx, compiledContract), Is.False);
//			}
//		}

//		[Test]
//		public void ShouldNotActivateHighVersionContract()
//		{
//			var highVContract = Consensus.Types.ExtendedContract.NewHighVContract(10, new byte[] { });
//			var tx = Utils.GetTx().AddOutput(Utils.GetContractSacrificeLock(new byte[] { }, 10)).SetContract(highVContract);

//			Assert.That(HandleBlock(_GenesisBlock.Child().AddTx(tx)), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));

//			using (var dbTx = _BlockChain.GetDBTransaction())
//			{
//				Assert.That(new ActiveContractSet().IsActive(dbTx, compiledContract), Is.False);
//			}
//		}

//		//cannot specify non-zen amount in SacrificeLock
//		//[Test]
//		//public void ShouldNotActivateNonZenSacrificedContract()
//		//{
//		//}

//		[Test]
//		public void ShouldActivateContractOfSameTx()
//		{
//			var kalapasPerBlock = (ulong)contractCode.Length * 1000 * 2;
//			var tx = Utils.GetTx().AddOutput(Utils.GetContractSacrificeLock(new byte[] { }, kalapasPerBlock)).SetContract(
//				new Consensus.Types.Contract(Encoding.ASCII.GetBytes(contractCode), new byte[] { }, new byte[] { }));

//			Assert.That(HandleBlock(_GenesisBlock.Child().AddTx(tx)), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));

//			using (var dbTx = _BlockChain.GetDBTransaction())
//			{
//				Assert.That(new ActiveContractSet().IsActive(dbTx, compiledContract), Is.True);
//			}
//		}

//		//[Test]
//		//public void ShouldActivateReferencesContract()
//		//{
//		//}
//	}
//}