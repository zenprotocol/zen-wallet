using System;
using BlockChain.Data;
using NUnit.Framework;
using Wallet.core.Data;
using System.Linq;

namespace BlockChain
{
	public class MempoolTests : BlockChainTestsBase
	{
#if CSHARP_CONTRACTS
			string contractCode = @"
    using System;
    using System.Collections.Generic;
	using Microsoft.FSharp.Core;
	using static Consensus.Types;

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
	string contractCode = @"
module Test
open Consensus.Types
let run (hash : byte[], utxos: Map<Outpoint, Output>, message: byte[]) = (utxos |> Map.toSeq |> Seq.map fst, utxos |> Map.toSeq |> Seq.map snd, Option<ExtendedContract>.None)
";
#endif

		[SetUp]
		public void SetUp()
		{
			OneTimeSetUp();
		}

		[Test]
		public void ShouldRemoveUnorphanInvalidTx()
		{
			var tx = Utils.GetTx().Sign().Tag("tx");
			var txInvalidOrphan = Utils.GetTx().AddInput(tx, 0).Tag("txInvalidOrphan");

			HandleTransaction(txInvalidOrphan);
			Assert.That(_BlockChain.memPool.OrphanTxPool.ContainsKey(txInvalidOrphan.Key()), Is.True, "should be there");
			HandleTransaction(tx);

			System.Threading.Thread.Sleep(100); // todo use wait

			Assert.That(_BlockChain.memPool.OrphanTxPool.ContainsKey(txInvalidOrphan.Key()), Is.False, "should not be there");
		}

		[Test]
		public void ShouldRemoveUnorphanInvalidTxWithDependencies()
		{
			var key = Key.Create();
			var tx = Utils.GetTx().Tag("tx");
			var txInvalidOrphan = Utils.GetTx().AddInput(tx, 1000).AddOutput(key.Address, Consensus.Tests.zhash, 100).Tag("Invalid Orphan");
			var txOrphanDepenent = Utils.GetTx().AddInput(txInvalidOrphan, 0).Tag("Orphan Depenent");

			HandleTransaction(txInvalidOrphan);
			HandleTransaction(txOrphanDepenent);
			HandleTransaction(tx);

			System.Threading.Thread.Sleep(1000); // todo use wait

			Assert.That(_BlockChain.memPool.OrphanTxPool.ContainsKey(txInvalidOrphan.Key()), Is.False, "Invalid orphan Tx should not be there");
			Assert.That(_BlockChain.memPool.OrphanTxPool.ContainsKey(txOrphanDepenent.Key()), Is.False, "Orphan dependent Tx should not be there");
		}

		[Test]
		public void ShouldNotUnorphanDoubleSpend()
		{
			var key = Key.Create();
			var tx = Utils.GetTx().AddOutput(key.Address, Consensus.Tests.zhash, 100).Tag("tx");
			var tx1 = Utils.GetTx().AddInput(tx, 0).AddOutput(key.Address, Consensus.Tests.zhash, 1).Tag("tx1");
			var tx2 = Utils.GetTx().AddInput(tx, 0).AddOutput(key.Address, Consensus.Tests.zhash, 2).Tag("tx2");

			HandleTransaction(tx1);
			HandleTransaction(tx2);
			HandleTransaction(tx);

			Assert.That(_BlockChain.memPool.TxPool.ContainsKey(tx1.Key()) &&
			            _BlockChain.memPool.TxPool.ContainsKey(tx2.Key()), Is.False, "both should not be in mempool");
		}
	
		[Test]
		public void ShouldInvalidateDoubleSpendOnNewBlock()
		{
			var key = Key.Create();
			var tx = Utils.GetTx().AddOutput(Key.Create().Address, Consensus.Tests.zhash, 100).Sign(key.Private).Tag("tx");
			var tx1 = Utils.GetTx().AddInput(tx, 0).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 1).Sign(key.Private).Tag("tx1");
			var tx2 = Utils.GetTx().AddInput(tx, 0).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 2).Sign(key.Private).Tag("tx2");

			HandleTransaction(tx);
			HandleTransaction(tx2);
			HandleBlock(_GenesisBlock.AddTx(tx).AddTx(tx1));

			Assert.That(_BlockChain.memPool.TxPool.ContainsKey(tx.Key()), Is.False, "should not be there");
			Assert.That(_BlockChain.memPool.TxPool.ContainsKey(tx1.Key()), Is.False, "should not be there");
			Assert.That(_BlockChain.memPool.TxPool.ContainsKey(tx2.Key()), Is.False, "should not be there");
		}

		[Test]
		public void ShouldInvalidateDoubleSpendOnNewBlockWithDependencies()
		{
			var key = Key.Create();
			var tx = Utils.GetTx().AddOutput(Key.Create().Address, Consensus.Tests.zhash, 100).Sign(key.Private).Tag("tx");
			var tx1 = Utils.GetTx().AddInput(tx, 0).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 1).Sign(key.Private).Tag("tx1");
			var tx2 = Utils.GetTx().AddInput(tx, 0).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 2).Sign(key.Private).Tag("tx2");
			var tx3 = Utils.GetTx().AddInput(tx2, 0).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 3).Sign(key.Private).Tag("tx3");

			HandleTransaction(tx);
			HandleTransaction(tx1);
			HandleTransaction(tx2);
			HandleTransaction(tx3);

			HandleBlock(_GenesisBlock.AddTx(tx).AddTx(tx1));

			Assert.That(_BlockChain.memPool.TxPool.ContainsKey(tx1.Key()), Is.False, "should not be there");
			Assert.That(_BlockChain.memPool.TxPool.ContainsKey(tx2.Key()), Is.False, "should not be there");
			Assert.That(_BlockChain.memPool.TxPool.ContainsKey(tx3.Key()), Is.False, "should not be there");
		}

		[Test]
		public void ShouldEvictWithDependencies()
		{
			var key = Key.Create();
			var tx = Utils
				.GetTx().AddOutput(Key.Create().Address, Consensus.Tests.zhash, 100)
				.Sign(key.Private).Tag("tx");
			var tx1 = Utils.GetTx().AddInput(tx, 0).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 1).Sign(key.Private).Tag("tx1");
			var tx2 = Utils.GetTx().AddInput(tx, 0).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 2).Sign(key.Private).Tag("tx2");

			var contractHash = new byte[] { };
			BlockChainTrace.SetTag(contractHash, "mock contract");
			_BlockChain.memPool.ContractPool.AddRef(tx1.Key(), new ACSItem() { Hash = contractHash });
			HandleBlock(_GenesisBlock.AddTx(tx).Tag("genesis"));
			Assert.That(TxState(tx), Is.EqualTo(TxStateEnum.Confirmed));
			HandleBlock(_GenesisBlock.Child().AddTx(tx1).Tag("main"));
			Assert.That(TxState(tx1), Is.EqualTo(TxStateEnum.Confirmed));
			var branch = _GenesisBlock.Child().Tag("branch");
			HandleBlock(branch.Child().Tag("branch orphan"));
			HandleBlock(branch.AddTx(tx2).Tag("branch child"));


			System.Threading.Thread.Sleep(1000);


			Assert.That(TxState(tx2), Is.EqualTo(TxStateEnum.Confirmed));
			Assert.That(TxState(tx1), Is.EqualTo(TxStateEnum.Invalid));
			Assert.That(_BlockChain.memPool.TxPool.ContainsKey(tx1.Key()), Is.False);
		}

		[Test]
		public void ShouldNotEvictDoubleSpend()
		{
			var key = Key.Create();
			var tx = Utils
				.GetTx().AddOutput(Key.Create().Address, Consensus.Tests.zhash, 100)
				.Sign(key.Private).Tag("tx");
			var cannotEvict = Utils.GetTx().AddInput(tx, 0).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 1).Sign(key.Private)
								 .Tag("cannotEvict");
			var invalidatingTx = Utils.GetTx().AddInput(tx, 0).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 2).Sign(key.Private)
									 .Tag("invalidatingTx");

			HandleBlock(_GenesisBlock.AddTx(tx).Tag("genesis"));
			Assert.That(TxState(tx), Is.EqualTo(TxStateEnum.Confirmed));
			HandleBlock(_GenesisBlock.Child().AddTx(cannotEvict).Tag("main"));
			Assert.That(TxState(cannotEvict), Is.EqualTo(TxStateEnum.Confirmed));
			var branch = _GenesisBlock.Child().Tag("branch");
			HandleBlock(branch.Child().AddTx(invalidatingTx).Tag("branch orphan"));
			HandleBlock(branch.Tag("branch child"));
			System.Threading.Thread.Sleep(100);
			Assert.That(TxState(cannotEvict), Is.EqualTo(TxStateEnum.Invalid));
			Assert.That(TxState(invalidatingTx), Is.EqualTo(TxStateEnum.Confirmed));
			Assert.That(_BlockChain.memPool.TxPool.Count, Is.EqualTo(0));
		}

		[Test]
		public void ShouldBeICTx()
		{
			//var key = Key.Create();

			//var contractHash = GetCompliedContract(contractCode);
			//BlockChainTrace.SetTag(contractHash, "contract");
			////			AddToACS(contractHash, contractCode, _GenesisBlock.header.blockNumber + 2);

			//var contractOutput1 = Utils.GetContractOutput(contractHash, new byte[] { }, Consensus.Tests.zhash, 11);

			//var tx = Utils
			//	.GetTx()
			//	.AddOutput(contractOutput1)
			//	.AddOutput(Key.Create().Address, Consensus.Tests.zhash, 100)
			//	.Sign(key.Private).Tag("tx");

			//HandleBlock(_GenesisBlock.AddTx(tx).Tag("genesis"));

			//var contractGeneratedTx = ExecuteContract(contractHash).Tag("contractGeneratedTx");
			//HandleTransaction(contractGeneratedTx);

			//Assert.That(_BlockChain.memPool.TxPool.Count(), Is.EqualTo(0));
			//Assert.That(_BlockChain.memPool.ICTxPool.Count(), Is.EqualTo(1));
		}

		[Test]
		public void ShouldMoveToICTxPoolWhenExpiring()
		{
			//var key = Key.Create();

			//var contractHash = GetCompliedContract(contractCode);
			//BlockChainTrace.SetTag(contractHash, "contract");
			//AddToACS(contractHash, contractCode, _GenesisBlock.header.blockNumber + 1);

			//var contractOutput1 = Utils.GetContractOutput(contractHash, new byte[] { }, Consensus.Tests.zhash, 11);

			//var tx = Utils
			//	.GetTx()
			//	.AddOutput(contractOutput1)
			//	.AddOutput(Key.Create().Address, Consensus.Tests.zhash, 100)
			//	.Sign(key.Private).Tag("tx");

			//var genesis = _GenesisBlock.AddTx(tx).Tag("genesis");
			//HandleBlock(genesis);

			//var contractGeneratedTx = ExecuteContract(contractHash).Tag("contractGeneratedTx");
			//HandleTransaction(contractGeneratedTx);

			//Assert.That(_BlockChain.memPool.TxPool.Count(), Is.EqualTo(1));
			//Assert.That(_BlockChain.memPool.ICTxPool.Count(), Is.EqualTo(0));
			//HandleBlock(genesis.Child().Tag("child"));
			//Assert.That(_BlockChain.memPool.TxPool.Count(), Is.EqualTo(0));
			//Assert.That(_BlockChain.memPool.ICTxPool.Count(), Is.EqualTo(1));
		}

//		[Test]
//		public void _ShouldNotEvictDoubleSpendWithDependencies()
//		{
//			var key = Key.Create();

//			string contractCode = @"
//module Test
//open Consensus.Types
//let run (context : ContractContext, message: byte[], outputs: Output list) = (context.utxo |> Map.toSeq |> Seq.map fst, outputs)
//";

//			var contractHash = GetCompliedContract(contractCode);
//			BlockChainTrace.SetTag(contractHash, "contract");
//		//	AddToACS(contractHash, contractCode, _GenesisBlock.header.blockNumber + 2);

//			var contractOutput1 = Utils.GetContractOutput(contractHash, new byte[] { }, Consensus.Tests.zhash, 11);
//			var contractOutput2 = Utils.GetContractOutput(contractHash, new byte[] { }, Consensus.Tests.zhash, 12);
//			var contractOutput3 = Utils.GetContractOutput(contractHash, new byte[] { }, Consensus.Tests.zhash, 13);

//			var tx = Utils
//				.GetTx()
//				.AddOutput(contractOutput1)
//				.AddOutput(contractOutput2)
//				.AddOutput(contractOutput3)
//				.AddOutput(Key.Create().Address, Consensus.Tests.zhash, 100)
//				.Sign(key.Private).Tag("tx");
//			var cannotEvict = Utils.GetTx().AddInput(tx, 0).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 1).Sign(key.Private)
//			                     .Tag("cannotEvict");
//			var invalidatingTx = Utils.GetTx().AddInput(tx, 0).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 2).Sign(key.Private)
//			                         .Tag("invalidatingTx");

//			_BlockChain.memPool.ContractPool.AddRef(cannotEvict.Key(), new ACSItem() { Hash = contractHash });
//			HandleBlock(_GenesisBlock.AddTx(tx).Tag("genesis"));
//			Assert.That(TxState(tx), Is.EqualTo(TxStateEnum.Confirmed));
//			HandleBlock(_GenesisBlock.Child().AddTx(cannotEvict).Tag("main"));
//			Assert.That(TxState(cannotEvict), Is.EqualTo(TxStateEnum.Confirmed));

//			var contractGeneratedTx = ExecuteContract(contractHash).Tag("contractGeneratedTx");
//			_BlockChain.HandleTransaction(contractGeneratedTx);

//			var branch = _GenesisBlock.Child().Tag("branch");
//			HandleBlock(branch.Child().AddTx(invalidatingTx).Tag("branch orphan"));
//			HandleBlock(branch.Tag("branch child"));
//			System.Threading.Thread.Sleep(200);
//			Assert.That(TxState(cannotEvict), Is.EqualTo(TxStateEnum.Invalid));
//			Assert.That(TxState(invalidatingTx), Is.EqualTo(TxStateEnum.Confirmed));

//			Assert.That(_BlockChain.memPool.TxPool.Count, Is.EqualTo(0));
//			Assert.That(_BlockChain.memPool.ContractPool.Count, Is.EqualTo(0));
//		}

		[Test]
		public void ShouldRemoveConfirmedFromMempoolWithDependencies()
		{
			var key = Key.Create();
			var tx = Utils
				.GetTx().AddOutput(Key.Create().Address, Consensus.Tests.zhash, 100)
				.Sign(key.Private).Tag("tx");
			var mempoolTx = Utils.GetTx().AddInput(tx, 0).AddOutput(Key.Create().Address, Consensus.Tests.zhash, 1).Sign(key.Private)
								 .Tag("mempoolTx");

			var contractHash = new byte[] { };
			BlockChainTrace.SetTag(contractHash, "mock contract");
			HandleBlock(_GenesisBlock.AddTx(tx).Tag("genesis"));
			HandleTransaction(mempoolTx);
			_BlockChain.memPool.ContractPool.AddRef(mempoolTx.Key(), new ACSItem() { Hash = contractHash });
			Assert.That(_BlockChain.memPool.TxPool.Count, Is.EqualTo(1));
			Assert.That(_BlockChain.memPool.ContractPool.Count, Is.EqualTo(1));
			HandleBlock(_GenesisBlock.Child().AddTx(mempoolTx).Tag("genesis"));
			Assert.That(_BlockChain.memPool.TxPool.Count, Is.EqualTo(0));
			Assert.That(_BlockChain.memPool.ContractPool.Count, Is.EqualTo(0));
		}
	}
}