//#if CSHARP_CONTRACTS
//using System;
//using System.Text;
//using NUnit.Framework;
//using Wallet.core.Data;

//namespace BlockChain
//{
//	public class CSharpContractTests : BlockChainContractTestsBase
//	{
// 		[Test]
//		public void TestCSharpContract()
//		{
//			HandleBlock(_GenesisBlock);

//			string contractCsCode = @"
//    using System;
//    using System.Collections.Generic;
//    using static Consensus.Types;
//    using Microsoft.FSharp.Core;

//    public class Test
//    {
//        public static Tuple<IEnumerable<Outpoint>, IEnumerable<Output>, FSharpOption<ExtendedContract>> run(
//			byte[] contractHash,
//			SortedDictionary<Outpoint, Output> utxos,
//			byte[] message
//		) {
//			Console.WriteLine(""Hello, world!"");

//			foreach (var item in utxos)
//			{
//				Console.WriteLine(""got an outpoint"");

//				Console.WriteLine(item.Value.spend.amount);
//				Console.WriteLine(item.Value.@lock);

//				var data = (item.Value.@lock as OutputLock.ContractLock).data;

//				if (data.Length > 0) 
//				{
//					Console.WriteLine(BitConverter.ToString(data));
					
//					var outpointsList = new List<Outpoint>();
//					outpointsList.Add(item.Key);

//					var newOutput = new Output(
//						OutputLock.NewPKLock(data),
//						new Spend(Consensus.Tests.zhash, 123)
//					);

//					var outputsList = new List<Output>();
//					outputsList.Add(newOutput);

//					return new Tuple<IEnumerable<Outpoint>, IEnumerable<Output>, FSharpOption<ExtendedContract>>(
//						outpointsList, outputsList, FSharpOption<ExtendedContract>.None
//					);
//				}
//			}

//			return null;
//        }
//    }";
//			byte[] contractHash;
//			ContractHelper.Compile(contractCsCode, out contractHash);

//			var autoTx = ExecuteContract(contractHash);

//			Assert.That(autoTx, Is.Null, "Should not generate auto-tx");

//			var address = Key.Create().Address;

//			Console.WriteLine(BitConverter.ToString(address.Bytes));

//			var kalapasPerBlock = (ulong)contractCsCode.Length * 1000 * 3;

//			var tx = Utils
//				.GetTx()
//				.AddOutput(Utils.GetContractSacrificeLock(new byte[] { }, kalapasPerBlock)).SetContract(
//					new Consensus.Types.Contract(Encoding.ASCII.GetBytes(contractCsCode), new byte[] { }, new byte[] { })
//				)
//				//		.AddOutput(Utils.GetContractOutput(contractHash, address.Bytes, Consensus.Tests.zhash, 123))
//				;

//			var newBlock = _GenesisBlock.Child().AddTx(tx);
//			Assert.That(HandleBlock(newBlock), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));

//			using (var dbTx = _BlockChain.GetDBTransaction())
//			{
//				Assert.That(new ActiveContractSet().IsActive(dbTx, contractHash), Is.True);
//			}

//			newBlock = newBlock.Child().AddTx(Utils.GetTx().AddOutput(Utils.GetContractOutput(contractHash, address.Bytes, Consensus.Tests.zhash, 123)));
//			Assert.That(HandleBlock(newBlock), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));


//			autoTx = ExecuteContract(contractHash);

//			Assert.That(autoTx, Is.Not.Null, "Should generate auto-tx");

//			Assert.That(_BlockChain.HandleTransaction(autoTx), Is.EqualTo(BlockChain.TxResultEnum.Accepted));

//			newBlock = newBlock.Child().AddTx(autoTx);
//			Assert.That(HandleBlock(newBlock), Is.EqualTo(BlockVerificationHelper.BkResultEnum.Accepted));
//		}
//	}
//}
//#endif