using NUnit.Framework;

namespace BlockChain
{
	public class ContractTests : BlockChainContractTestsBase
	{
		//byte[] compiledContract;
		//Types.Transaction contractCreatedTransaction;
		//List<Types.Output> outputs = new List<Types.Output>();
		//	List<Types.Outpoint> inputs = new List<Types.Outpoint>();

		string contractFsCode = @"
module Test
open Consensus.Types
let run (context : ContractContext, witnesses: Witness list, outputs: Output list, contract: ExtendedContract) = (context.utxo |> Map.toSeq |> Seq.map fst, witnesses, outputs, contract)
";

		[SetUp]
		public void Setup()
		{
			OneTimeSetUp();
		}


		public void ShouldActivateContract()
		{
		}

		public void ShouldExtendContract()
		{
			//Assert.That(_BlockChain.HandleBlock(_GenesisBlock), Is.True);

			//var compiledContract = GetCompliedContract(contractFsCode);

			//AddToActiveContactsSet(compiledContract);

			//var output = Utils.GetContractOutput(contractHash, null, null, null);
			//var tx = Utils.GetTx().AddOutput(output);
			//var bk = _GenesisBlock.Child().AddTx(tx);

			//Assert.That(_BlockChain.HandleBlock(bk), "Should add block", Is.True);

			//using (var dbTx = _BlockChain.GetDBTransaction())
			//{
			//	Assert.That(_BlockChain.ActiveContractSet.IsActive(dbTx, contractHash, ), Is.True);
			//}
		}

		[Test]
		public void ShouldExtendMempoolContract()
		{

		}

		[Test]
		public void ShouldNotExtendInvalidContract()
		{

		}

		//TODO: test peer banning
	}
}
