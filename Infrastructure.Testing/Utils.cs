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
		public static Keyed<Types.Block> GetGenesisBlock()
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

			var block = new Types.Block(header, ListModule.OfSeq<Types.Transaction>(new List<Types.Transaction>()));
			var key = Merkle.blockHeaderHasher.Invoke(header);

			return new Keyed<Types.Block>(key, block);
		}

		//protected Keyed<Types.Transaction> GetTx(Keyed<Types.Transaction> tx)
		//{
		//	return GetTx(tx.Key);
		//}

		//protected Keyed<Types.Transaction> GetTx(Types.Transaction tx)
		//{
		//	return GetTx(Merkle.transactionHasher.Invoke(tx));
		//}

		public static Types.Transaction GetTx(params Tuple<Types.Outpoint, byte[]>[] outpoints)
		{
			var outputs = new List<Types.Output>();

			var tx = new Types.Transaction(
				0,
				ListModule.OfSeq(new List<Types.Outpoint>()),
				ListModule.OfSeq(new List<byte[]>()),
				ListModule.OfSeq(outputs),
				null);


			//			var signedTx = TransactionValidation.signTx(tx, ListModule.OfSeq(outpoints.Select(o => o.Item2))); 

			//	var key = Merkle.transactionHasher.Invoke(tx);

			//	return new Keyed<Types.Transaction>(key, tx);

			return tx;
		}
	}
}
