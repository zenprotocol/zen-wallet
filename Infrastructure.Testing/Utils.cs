using System;
using System.Collections.Generic;
using System.Linq;
using Consensus;
using Microsoft.FSharp.Collections;
using Store;

namespace Infrastructure.Testing
{
	public class Utils
	{
		public static Types.Block GetGenesisBlock()
		{
			var nonce = new byte[10];

			new Random().NextBytes(nonce);

			var header = new Types.BlockHeader(
				0,
				new byte[] { },
				0,
				new byte[] { },
				new byte[] { },
				new byte[] { },
				ListModule.OfSeq<byte[]>(new List<byte[]>()),
				DateTime.Now.ToFileTimeUtc(),
				0,
				nonce
			);

			return new Types.Block(header, ListModule.OfSeq<Types.Transaction>(new List<Types.Transaction>()));
		}

		public static Types.Transaction GetTx()
		{
			return new Types.Transaction(
				0,
				ListModule.OfSeq(new List<Types.Outpoint>()),
				ListModule.OfSeq(new List<byte[]>()),
				ListModule.OfSeq(new List<Types.Output>()),
				null);
		}

		public static Types.Output GetOutput(byte[] address, byte[] asset, ulong amount)
		{
			return new Types.Output(
				Types.OutputLock.NewPKLock(address),
				new Types.Spend(asset, amount));
		}

		public static Types.Output GetContractOutput(byte[] contractHash, byte[] data, byte[] asset, ulong amount)
		{
			return new Types.Output(
				Types.OutputLock.NewContractLock(contractHash, data),
				new Types.Spend(asset, amount));
		}

		public static Types.Output GetContractSacrificeLock(byte[] contractHash, ulong zenAmount)
		{
			return new Types.Output(
				Types.OutputLock.NewContractSacrificeLock(
					new Types.LockCore(0, ListModule.OfSeq(new byte[][] { contractHash }))
				),
				new Types.Spend(Consensus.Tests.zhash, zenAmount)
			);
		}
	}
}
