using System;
using System.Collections.Generic;
using System.IO;
using Consensus;
using Infrastructure;
using NUnit.Framework;
using Store;
using Wallet.core;
using Wallet.core.Data;
using Microsoft.FSharp.Collections;

namespace Wallet.Tests
{
	public class Output
	{
		public Key Key { get; set; }
		public ulong Amount { get; set; }
		public byte[] Asset { get; set; }
	}

	public class Utils
	{
		public static List<Output> CreateOutputsList() 
		{
			var retValue = new List<Output>();

			retValue.Add(new Output() { Key = Key.Create(), Amount = (ulong)10, Asset = Consensus.Tests.zhash });
			retValue.Add(new Output() { Key = Key.Create(), Amount = (ulong)50, Asset = Consensus.Tests.zhash });
			retValue.Add(new Output() { Key = Key.Create(), Amount = (ulong)100, Asset = Consensus.Tests.zhash });

			return retValue;
		}

		public static Keyed<Types.Block> GetGenesisBlock(List<Output> outputsList)
		{
			var outputs = new List<Types.Output>();
			var inputs = new List<Types.Outpoint>();
			var hashes = new List<byte[]>();
			var version = (uint)1;
			var date = "2000-02-02";

			foreach (var output in outputsList)
			{
				var pklock = Types.OutputLock.NewPKLock(output.Key.Address);
				outputs.Add(new Types.Output(pklock, new Types.Spend(output.Asset, output.Amount)));
			}

			var transaction = new Types.Transaction(version,
				ListModule.OfSeq(inputs),
				ListModule.OfSeq(hashes),
				ListModule.OfSeq(outputs),
				null);

			var transactions = new List<Types.Transaction>();
			transactions.Add(transaction);

			var blockHeader = new Types.BlockHeader(
				version,
				new byte[] { },
				0,
				new byte[] { },
				new byte[] { },
				new byte[] { },
				ListModule.OfSeq<byte[]>(new List<byte[]>()),
				DateTime.Parse(date).ToBinary(),
				1,
				new byte[] { }
			);

			var block = new Types.Block(blockHeader, ListModule.OfSeq<Types.Transaction>(transactions));
			var blockHash = Merkle.blockHeaderHasher.Invoke(blockHeader);

			return new Keyed<Types.Block>(blockHash, block);
		}

		public static long GetBalance(WalletManager wallet, byte[] asset)
		{
			long total = 0;

			var balances = wallet.Import();

			if (balances.ContainsKey(asset))
			{
				foreach (var tx in balances[asset])
				{
					total += tx;
				}
			}

			return total;
		}
	}
}